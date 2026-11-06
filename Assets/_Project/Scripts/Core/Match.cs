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

        public static readonly IReadOnlyList<God> TurnOrder = Array.AsReadOnly(
            new[] { God.Zeus, God.Ra, God.Thor, God.Morrigan }
        );

        private readonly IRandom _random;
        private int _turnIndex;

        public Match(Board board, IRandom random)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Round number and Destiny face, before the bad omen (if any) is applied.</summary>
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
        public God CurrentGod => TurnOrder[_turnIndex];

        /// <summary>The Destiny Die face of the current round (0 before the match starts).</summary>
        public int DestinyFace { get; private set; }

        public int ReinforcementsLeft { get; private set; }

        /// <summary>The combat waiting for an Offering or confirmation; null outside CombatPending.</summary>
        public Combat PendingCombat { get; private set; }

        public God? Winner { get; private set; }
        public VictoryReason? WinReason { get; private set; }

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

        public static int CalculateReinforcements(int territories, int temples, int destinyModifier)
        {
            int total = BaseReinforcements + territories / 2 + temples + destinyModifier;
            return Math.Max(MinReinforcements, total);
        }

        /// <summary>Rolls Destiny for round 1 and begins Zeus's turn. All four nations start alive.</summary>
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

            int next = _turnIndex;
            bool wrapped = false;
            do
            {
                next++;
                if (next == TurnOrder.Count)
                {
                    next = 0;
                    wrapped = true;
                }
            } while (IsEliminated(TurnOrder[next]));

            if (wrapped && Round >= RoundLimit)
            {
                Win(RoundLimitWinner(), VictoryReason.RoundLimit);
                return;
            }

            // Set first so round events (Destiny, bad omen) already see the new round's first god.
            _turnIndex = next;
            if (wrapped)
                StartRound(Round + 1);
            StartTurn();
        }

        private void StartRound(int round)
        {
            Round = round;
            DestinyFace = Die.Roll(_random);
            RoundStarted?.Invoke(Round, DestinyFace);

            if (Destiny.IsBadOmen(DestinyFace))
                ApplyBadOmen();
        }

        private void ApplyBadOmen()
        {
            foreach (God god in TurnOrder)
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

            ReinforcementsLeft = CalculateReinforcements(
                Board.TerritoryCount(god),
                Board.TempleCount(god),
                Destiny.ReinforcementModifier(god, DestinyFace)
            );
            Phase = TurnPhase.Reinforce;
            TurnStarted?.Invoke(god, ReinforcementsLeft);
        }

        private void Capture(Territory from, Territory to)
        {
            God? previousOwner = to.Owner;
            to.Owner = CurrentGod;
            to.Troops = from.Troops - 1;
            from.Troops = 1;
            TerritoryChanged?.Invoke(from);
            TerritoryChanged?.Invoke(to);
            TerritoryCaptured?.Invoke(to, CurrentGod, previousOwner);

            if (previousOwner == null || !IsEliminated(previousOwner.Value))
                return;

            NationEliminated?.Invoke(previousOwner.Value);
            if (LivingNationCount() == 1)
                Win(CurrentGod, VictoryReason.LastStanding);
        }

        // Most territories, then most troops; iterating in turn order makes the earlier god win full ties.
        private God RoundLimitWinner()
        {
            God best = CurrentGod;
            int bestTerritories = -1;
            int bestTroops = -1;
            foreach (God god in TurnOrder)
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

        private int LivingNationCount()
        {
            int count = 0;
            foreach (God god in TurnOrder)
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
