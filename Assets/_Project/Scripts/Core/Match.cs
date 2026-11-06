using System;
using System.Collections.Generic;

namespace GameJam.Core
{
    /// <summary>
    /// One match of GodBoard: the game state plus the commands that change it.
    /// Turn flow is an explicit state machine over <see cref="TurnPhase"/>: every command checks
    /// the phase first and throws <see cref="InvalidOperationException"/> when it is not allowed.
    /// Events fire after the state they describe has changed. Handlers must only read state and
    /// must not issue commands synchronously, because the command that raised them is still running.
    /// </summary>
    public sealed class Match
    {
        public const int RoundLimit = 15;
        public const int TemplesToWin = 3;
        public const int BaseReinforcements = 2;
        public const int MinReinforcements = 1;
        public const int MinTroopsToAttack = 2;
        public const int MinTroopsToOffer = 3;

        /// <summary>An empty territory only takes a third of the source's troops (at least 1).</summary>
        public const int EmptyCaptureDivisor = 3;

        /// <summary>
        /// The clockwise cycle of the corners. Each round starts at a different god (see
        /// <see cref="RoundOpener"/>) and follows this cycle; the round-limit tie-break uses it as is.
        /// </summary>
        public static readonly IReadOnlyList<God> BaseTurnOrder = Array.AsReadOnly(
            new[] { God.Zeus, God.Ra, God.Thor, God.Morrigan }
        );

        private readonly IRandom _random;

        // Both are positions in BaseTurnOrder.
        private int _turnIndex;
        private int _openerIndex;

        public Match(Board board, IRandom random)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Round number and Destiny face, before the bad omen (if any) is applied.
        /// <see cref="RoundOpener"/> and <see cref="CurrentGod"/> already hold the new round's first god.
        /// </summary>
        public event Action<int, int> RoundStarted;

        /// <summary>God whose turn begins and the reinforcements it received.</summary>
        public event Action<God, int> TurnStarted;

        /// <summary>Raised whenever a territory's owner or troops change.</summary>
        public event Action<Territory> TerritoryChanged;

        /// <summary>Raised after the attack roll and again after an Offering re-roll.</summary>
        public event Action<Combat> CombatRolled;

        /// <summary>Raised after the loser lost its troop, before any capture.</summary>
        public event Action<Combat> CombatResolved;

        /// <summary>Territory, new owner, previous owner (null if it was unowned).</summary>
        public event Action<Territory, God, God?> TerritoryCaptured;

        public event Action<God> NationEliminated;

        public event Action<God, VictoryReason> MatchWon;

        public Board Board { get; }
        public int Round { get; private set; }
        public TurnPhase Phase { get; private set; }
        public God CurrentGod => BaseTurnOrder[_turnIndex];

        /// <summary>The god who plays first this round, chosen by the Destiny Die.</summary>
        public God RoundOpener => BaseTurnOrder[_openerIndex];

        /// <summary>The Destiny Die face of the current round (0 before the match starts).</summary>
        public int DestinyFace { get; private set; }

        public int ReinforcementsLeft { get; private set; }

        /// <summary>How the current turn's reinforcements were earned, so the UI can explain the total.</summary>
        public Reinforcements TurnReinforcements { get; private set; }

        /// <summary>The combat waiting for an Offering or confirmation; null outside CombatPending.</summary>
        public Combat PendingCombat { get; private set; }

        public God? Winner { get; private set; }
        public VictoryReason? WinReason { get; private set; }

        /// <summary>What decided a round-limit win, so the UI can explain it; null for any other ending.</summary>
        public RoundLimitDecider? RoundLimitDecider { get; private set; }

        // A nation with no territories can never get one back, so ownership is the source of truth.
        public bool IsEliminated(God god) => Board.TerritoryCount(god) == 0;

        // The Offering is a comeback tool: only while the attacker is losing the rolled combat.
        public bool CanMakeOffering =>
            Phase == TurnPhase.CombatPending
            && !PendingCombat.OfferingMade
            && PendingCombat.Result.Winner == CombatSide.Defender
            && PendingCombat.From.Troops >= MinTroopsToOffer;

        // Single source of the attack rules: UI highlights and bots ask, Attack enforces.
        public bool CanAttack(Territory from, Territory to) =>
            Phase == TurnPhase.Attack
            && IsOnBoard(from)
            && IsOnBoard(to)
            && from.Owner == CurrentGod
            && to.Owner != CurrentGod
            && from.IsAdjacentTo(to)
            && from.Troops >= MinTroopsToAttack;

        /// <summary>Troops that move into an empty territory; the source always keeps at least 1.</summary>
        public static int EmptyCaptureTroops(int sourceTroops) =>
            Math.Max(1, sourceTroops / EmptyCaptureDivisor);

        /// <summary>
        /// Position of a god in <see cref="BaseTurnOrder"/>, which is also its corner clockwise from top-left.
        /// </summary>
        public static int BaseTurnIndex(God god)
        {
            for (int i = 0; i < BaseTurnOrder.Count; i++)
            {
                if (BaseTurnOrder[i] == god)
                    return i;
            }
            throw new ArgumentOutOfRangeException(nameof(god), god, null);
        }

        /// <summary>Rolls Destiny for round 1 and begins the opener's turn. All four nations start alive.</summary>
        public void Start()
        {
            RequirePhase(TurnPhase.NotStarted, "start the match");
            StartRound(1);
            StartTurn();
        }

        public void PlaceTroop(Territory territory)
        {
            RequireTerritory(territory, nameof(territory));
            RequirePhase(TurnPhase.Reinforce, "place troops");
            if (territory.Owner != CurrentGod)
                throw new InvalidOperationException(
                    $"{CurrentGod} can only place troops on its own territories, not {territory.Name}."
                );

            territory.Troops++;
            ReinforcementsLeft--;
            TerritoryChanged?.Invoke(territory);

            if (ReinforcementsLeft == 0)
                Phase = TurnPhase.Attack;
        }

        public void Attack(Territory from, Territory to)
        {
            RequirePhase(TurnPhase.Attack, "attack");
            RequireTerritory(from, nameof(from));
            RequireTerritory(to, nameof(to));
            if (!CanAttack(from, to))
                throw new InvalidOperationException($"Illegal attack {from} -> {to}.");

            // Unowned territories are always empty, so they are taken without a fight.
            if (to.Owner == null)
            {
                Capture(from, to);
                return;
            }

            God defender = to.Owner.Value;
            CombatResult result = CombatResolver.Roll(CurrentGod, defender, _random);
            PendingCombat = new Combat(from, to, CurrentGod, defender, result);
            Phase = TurnPhase.CombatPending;
            CombatRolled?.Invoke(PendingCombat);
        }

        public void MakeOffering()
        {
            RequirePhase(TurnPhase.CombatPending, "make an Offering");
            if (!CanMakeOffering)
                throw new InvalidOperationException(
                    "Offering needs 3+ troops, a losing roll and is once per combat."
                );

            Combat combat = PendingCombat;
            combat.From.Troops--;
            combat.OfferingMade = true;
            TerritoryChanged?.Invoke(combat.From);

            // Only the attacker re-rolls; the defender keeps the die already on the table.
            int attackerRoll = Die.Roll(_random);
            combat.Result = CombatResolver.Resolve(
                combat.Attacker,
                attackerRoll,
                combat.Defender,
                combat.Result.DefenderRoll
            );
            CombatRolled?.Invoke(combat);
        }

        public void ConfirmCombat()
        {
            RequirePhase(TurnPhase.CombatPending, "confirm a combat");
            Combat combat = PendingCombat;

            Territory loser = combat.Result.Winner == CombatSide.Attacker ? combat.To : combat.From;
            loser.Troops--;

            // A defender left with 0 troops is captured at once; Capture reports both territories.
            bool captured = combat.To.Troops == 0;
            if (!captured)
                TerritoryChanged?.Invoke(loser);
            CombatResolved?.Invoke(combat);

            if (captured)
                Capture(combat.From, combat.To);

            PendingCombat = null;
            if (Phase != TurnPhase.MatchOver)
                Phase = TurnPhase.Attack;
        }

        public void EndTurn()
        {
            RequirePhase(TurnPhase.Attack, "end the turn");

            if (TryFindNextInRound(out int next))
            {
                _turnIndex = next;
                StartTurn();
                return;
            }

            if (Round >= RoundLimit)
            {
                God winner = RoundLimitWinner();
                RoundLimitDecider = DecidingStep(winner);
                Win(winner, VictoryReason.RoundLimit);
                return;
            }

            StartRound(Round + 1);
            StartTurn();
        }

        // Walks the cycle from the current god. Reaching the opener's seat again means every living
        // god has played; checking the seat (not the god) also works if the opener died this round.
        private bool TryFindNextInRound(out int next)
        {
            for (int step = 1; step <= BaseTurnOrder.Count; step++)
            {
                int candidate = (_turnIndex + step) % BaseTurnOrder.Count;
                if (candidate == _openerIndex)
                    break;
                if (!IsEliminated(BaseTurnOrder[candidate]))
                {
                    next = candidate;
                    return true;
                }
            }
            next = -1;
            return false;
        }

        private void StartRound(int round)
        {
            Round = round;
            DestinyFace = Die.Roll(_random);

            // Set before the event so round listeners already see who opens the new round.
            _openerIndex = FirstLivingFrom(DestinyOpener(round));
            _turnIndex = _openerIndex;
            RoundStarted?.Invoke(Round, DestinyFace);

            if (Destiny.IsBadOmen(DestinyFace))
                ApplyBadOmen();
        }

        // A god's face lets that god open the round. 3 and 4 belong to everyone, so the opener just
        // rotates one seat; round 1 has no previous opener and starts with Zeus.
        private int DestinyOpener(int round)
        {
            God? favored = Pantheon.GodOfFace(DestinyFace);
            if (favored != null)
                return BaseTurnIndex(favored.Value);
            return round == 1 ? 0 : (_openerIndex + 1) % BaseTurnOrder.Count;
        }

        // The match ends before fewer than two gods are alive, so a living god is always found.
        private int FirstLivingFrom(int index)
        {
            while (IsEliminated(BaseTurnOrder[index]))
                index = (index + 1) % BaseTurnOrder.Count;
            return index;
        }

        private void ApplyBadOmen()
        {
            foreach (God god in BaseTurnOrder)
            {
                Territory largest = LargestArmy(god);
                if (largest == null || largest.Troops < Destiny.BadOmenMinTroops)
                    continue;

                largest.Troops--;
                TerritoryChanged?.Invoke(largest);
            }
        }

        // Territories are ordered by id, so a strict comparison keeps the lowest id on ties.
        private Territory LargestArmy(God god)
        {
            Territory largest = null;
            foreach (Territory territory in Board.Territories)
            {
                if (
                    territory.Owner == god
                    && (largest == null || territory.Troops > largest.Troops)
                )
                    largest = territory;
            }
            return largest;
        }

        private void StartTurn()
        {
            God god = CurrentGod;

            if (Board.TempleCount(god) >= TemplesToWin)
            {
                Win(god, VictoryReason.Temples);
                return;
            }

            TurnReinforcements = new Reinforcements(
                Board.TerritoryCount(god),
                Board.TempleCount(god),
                Destiny.ReinforcementModifier(god, DestinyFace)
            );
            ReinforcementsLeft = TurnReinforcements.Total;
            Phase = TurnPhase.Reinforce;
            TurnStarted?.Invoke(god, ReinforcementsLeft);
        }

        private void Capture(Territory from, Territory to)
        {
            God? previousOwner = to.Owner;
            to.Owner = CurrentGod;

            // A won combat moves in all but one. An empty territory is free, so it only takes a third:
            // grabbing land never strips the front line.
            int moved = previousOwner == null ? EmptyCaptureTroops(from.Troops) : from.Troops - 1;
            to.Troops = moved;
            from.Troops -= moved;
            TerritoryChanged?.Invoke(from);
            TerritoryChanged?.Invoke(to);
            TerritoryCaptured?.Invoke(to, CurrentGod, previousOwner);

            if (previousOwner == null || !IsEliminated(previousOwner.Value))
                return;

            NationEliminated?.Invoke(previousOwner.Value);
            if (LivingNationCount() == 1)
                Win(CurrentGod, VictoryReason.LastStanding);
        }

        // Most territories, then most troops; iterating the base cycle makes the earlier god win full ties.
        private God RoundLimitWinner()
        {
            God best = CurrentGod;
            int bestTerritories = -1;
            int bestTroops = -1;
            foreach (God god in BaseTurnOrder)
            {
                int territories = Board.TerritoryCount(god);
                int troops = Board.TotalTroops(god);
                bool better =
                    territories > bestTerritories
                    || (territories == bestTerritories && troops > bestTroops);
                if (!better)
                    continue;

                best = god;
                bestTerritories = territories;
                bestTroops = troops;
            }
            return best;
        }

        // The first ranking step where nobody else matches the winner is the one that decided it.
        private RoundLimitDecider DecidingStep(God winner)
        {
            int territories = Board.TerritoryCount(winner);
            int troops = Board.TotalTroops(winner);
            bool tiedOnTerritories = false;
            bool tiedOnTroops = false;
            foreach (God god in BaseTurnOrder)
            {
                if (god == winner || Board.TerritoryCount(god) != territories)
                    continue;
                tiedOnTerritories = true;
                if (Board.TotalTroops(god) == troops)
                    tiedOnTroops = true;
            }

            if (!tiedOnTerritories)
                return Core.RoundLimitDecider.Territories;
            return tiedOnTroops ? Core.RoundLimitDecider.TurnOrder : Core.RoundLimitDecider.Troops;
        }

        private int LivingNationCount()
        {
            int count = 0;
            foreach (God god in BaseTurnOrder)
            {
                if (!IsEliminated(god))
                    count++;
            }
            return count;
        }

        private void Win(God god, VictoryReason reason)
        {
            Winner = god;
            WinReason = reason;
            PendingCombat = null;
            Phase = TurnPhase.MatchOver;
            MatchWon?.Invoke(god, reason);
        }

        private void RequirePhase(TurnPhase expected, string action)
        {
            if (Phase != expected)
                throw new InvalidOperationException($"Cannot {action} during the {Phase} phase.");
        }

        private bool IsOnBoard(Territory territory) =>
            territory != null && Board.Get(territory.Id) == territory;

        private void RequireTerritory(Territory territory, string paramName)
        {
            if (territory == null)
                throw new ArgumentNullException(paramName);
            if (!IsOnBoard(territory))
                throw new ArgumentException(
                    $"{territory.Name} does not belong to this match's board.",
                    paramName
                );
        }
    }
}
