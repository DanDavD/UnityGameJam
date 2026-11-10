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

        /// <summary>Troops an Offering takes from the attacking territory.</summary>
        public const int OfferingCost = 1;

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

        // Indexed by (int)God; always 0 while powers are off.
        private readonly int[] _favor = new int[BaseTurnOrder.Count];

        // Both are positions in BaseTurnOrder.
        private int _turnIndex;
        private int _openerIndex;

        private bool _wrathUsedThisTurn;

        // The Sacred Road: opened by the attacker's sacred final die, gone at the end of the turn.
        private bool _roadAvailable;

        // Caps on what Morrígan can gather: crows per round, Harvest per unleash (one per turn).
        private int _crowsThisRound;
        private int _harvestedThisTurn;

        /// <summary>The classic rules: no Day of the God, no Favor, no Wrath.</summary>
        public Match(Board board, IRandom random)
            : this(board, random, powersEnabled: false) { }

        /// <summary>
        /// <paramref name="powersEnabled"/> turns on the god powers (menu "Pantheon powers"):
        /// the Day of the God, Favor and the Wrath. They draw no extra random numbers.
        /// </summary>
        public Match(Board board, IRandom random, bool powersEnabled)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            PowersEnabled = powersEnabled;
        }

        /// <summary>
        /// Round number and Destiny face, before the bad omen or the Day of the God is applied.
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

        /// <summary>
        /// A god's Day begins, right after <see cref="RoundStarted"/> and before its instant effect.
        /// The list holds the territories that effect is about to change (Storm: struck enemies,
        /// whose troops then join <see cref="GodPowers.StormDestination"/>; Sun at noon: Ra's
        /// territories); it is empty for Ragnarök and Night of crows, which last the whole round.
        /// Their <see cref="TerritoryChanged"/> events follow.
        /// </summary>
        public event Action<God, IReadOnlyList<Territory>> DayStarted;

        /// <summary>A nation's Favor changed: god and new value (0 to <see cref="GodPowers.MaxFavor"/>).</summary>
        public event Action<God, int> FavorChanged;

        /// <summary>
        /// A god spent its Favor on its Wrath, before the effect is applied. The list holds the
        /// territories about to be hit (Thunderbolt and Mjölnir: the target; Eye of Ra: the ones it
        /// converts; Harvest: none). <see cref="TerritoryChanged"/>, <see cref="TerritoryCaptured"/>,
        /// <see cref="NationEliminated"/> and <see cref="MatchWon"/> follow as they apply.
        /// </summary>
        public event Action<God, IReadOnlyList<Territory>> WrathUnleashed;

        /// <summary>
        /// The attacker's final die was its sacred number: the gods open one Road for the rest of its
        /// turn. Raised at the end of <see cref="ConfirmCombat"/>, once the phase is Attack again.
        /// </summary>
        public event Action<God> RoadOpened;

        /// <summary>
        /// A Sacred Road march happened: god, origin, destination, the shortest path of own territories
        /// between them (both included) and the troops moved. Raised after the two
        /// <see cref="TerritoryChanged"/> events of the endpoints.
        /// </summary>
        public event Action<God, Territory, Territory, IReadOnlyList<Territory>, int> RoadUsed;

        public Board Board { get; }

        /// <summary>True when the god powers (Day, Favor, Wrath) are part of this match.</summary>
        public bool PowersEnabled { get; }

        /// <summary>
        /// The god whose Day lasts this round: the living god on the Destiny face. Null on 3 and 4,
        /// when that god is eliminated, and always while powers are off.
        /// </summary>
        public God? DayGod { get; private set; }

        /// <summary>Thor's Day: Thor wins ties and, when he wins, the loser loses 2.</summary>
        public bool IsRagnarokActive => DayGod == God.Thor;

        /// <summary>Morrígan's Day: troops other nations lose in combat join her.</summary>
        public bool IsNightOfCrowsActive => DayGod == God.Morrigan;

        /// <summary>Morrígan unleashed her Harvest this turn: her attacks reap the defender's losses.</summary>
        public bool IsHarvestActive { get; private set; }

        /// <summary>Defender losses the active Harvest can still reap this turn; 0 without a Harvest.</summary>
        public int HarvestTroopsLeft =>
            IsHarvestActive ? GodPowers.HarvestMaxTroops - _harvestedThisTurn : 0;

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

        public int FavorOf(God god) => _favor[(int)god];

        /// <summary>The current god already unleashed its Wrath this turn (it is once per turn).</summary>
        public bool WrathUsedThisTurn => _wrathUsedThisTurn;

        /// <summary>
        /// The current god may unleash its Wrath now: full Favor, its own Attack phase with no combat
        /// pending, and not unleashed yet this turn. Per-god targets are checked by <see cref="CanUnleashWrath"/>.
        /// </summary>
        public bool IsWrathReady =>
            PowersEnabled
            && Phase == TurnPhase.Attack
            && !_wrathUsedThisTurn
            && FavorOf(CurrentGod) >= GodPowers.MaxFavor;

        /// <summary>
        /// Single source of the Wrath rules. Zeus: <paramref name="target"/> is any enemy territory
        /// with 2+ troops. Thor: <paramref name="source"/> and <paramref name="target"/> follow the
        /// attack rules, enemy target only. Ra: needs at least one Eye of Ra candidate. Morrígan: no
        /// target. Arguments a god does not use are ignored.
        /// </summary>
        public bool CanUnleashWrath(Territory source = null, Territory target = null)
        {
            if (!IsWrathReady)
                return false;

            switch (CurrentGod)
            {
                case God.Zeus:
                    return IsEnemyOnBoard(target)
                        && target.Troops >= GodPowers.ThunderboltMinTroops;
                case God.Thor:
                    return CanAttack(source, target) && target.Owner != null;
                case God.Ra:
                    return GodPowers.EyeOfRaTargets(Board).Count > 0;
                default:
                    return true;
            }
        }

        /// <summary>
        /// The current god holds an unused Sacred Road this turn. Always false in classic matches
        /// and once the match is over.
        /// </summary>
        public bool IsRoadAvailable => _roadAvailable;

        /// <summary>
        /// Single source of the Road rules: the Road is open, it is the current god's Attack phase
        /// (so no combat is pending), <paramref name="from"/> is its own with 2+ troops and
        /// <paramref name="to"/> is one of <see cref="GodPowers.RoadDestinations"/>.
        /// </summary>
        public bool CanUseRoad(Territory from, Territory to) =>
            _roadAvailable
            && Phase == TurnPhase.Attack
            && IsOnBoard(from)
            && IsOnBoard(to)
            && from.Owner == CurrentGod
            && from.Troops >= MinTroopsToAttack
            && GodPowers.RoadDestinations(Board, CurrentGod, from).Contains(to);

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
            int attackerRoll = Die.Roll(_random);
            int defenderRoll = Die.Roll(_random);
            CombatResult result = ResolveCombat(from, to, defender, attackerRoll, defenderRoll);
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
            combat.From.Troops -= OfferingCost;
            combat.OfferingMade = true;
            TerritoryChanged?.Invoke(combat.From);

            // Only the attacker re-rolls; the defender keeps the die already on the table.
            int attackerRoll = Die.Roll(_random);
            combat.Result = ResolveCombat(
                combat.From,
                combat.To,
                combat.Defender,
                attackerRoll,
                combat.Result.DefenderRoll
            );
            CombatRolled?.Invoke(combat);
        }

        public void ConfirmCombat()
        {
            RequirePhase(TurnPhase.CombatPending, "confirm a combat");
            Combat combat = PendingCombat;
            bool attackerWon = combat.Result.Winner == CombatSide.Attacker;

            Territory loser = attackerWon ? combat.To : combat.From;
            int losses = combat.Result.LoserLosses;
            loser.Troops -= losses;

            // A defender left with 0 troops is captured at once; Capture reports both territories.
            bool captured = combat.To.Troops == 0;
            if (!captured)
                TerritoryChanged?.Invoke(loser);
            GatherLostTroops(combat, attackerWon, losses, captured);
            UpdateFavor(combat);
            CombatResolved?.Invoke(combat);

            if (captured)
                Capture(combat.From, combat.To);

            PendingCombat = null;
            if (Phase == TurnPhase.MatchOver)
                return;

            Phase = TurnPhase.Attack;
            OpenRoadIfSacred(combat);
        }

        // The same moment that earns Favor: the attacker's final die shows its own sacred number.
        // Only the attacker earns it (a defender's die never interrupts another nation's turn), and
        // there is at most one Road at a time.
        private void OpenRoadIfSacred(Combat combat)
        {
            if (!PowersEnabled || _roadAvailable)
                return;
            if (combat.Result.AttackerOutcome != RollOutcome.Sacred)
                return;

            _roadAvailable = true;
            RoadOpened?.Invoke(combat.Attacker);
        }

        /// <summary>
        /// Spends the current god's full Favor on its Wrath (see <see cref="CanUnleashWrath"/> for
        /// the arguments). The phase stays Attack unless the Wrath ends the match.
        /// </summary>
        public void UnleashWrath(Territory source = null, Territory target = null)
        {
            RequirePhase(TurnPhase.Attack, "unleash a Wrath");
            if (!CanUnleashWrath(source, target))
                throw new InvalidOperationException(
                    $"{CurrentGod} cannot unleash its Wrath: it needs {GodPowers.MaxFavor} Favor, "
                        + "once per turn, and a legal target."
                );

            God god = CurrentGod;
            _wrathUsedThisTurn = true;
            SetFavor(god, 0);

            switch (god)
            {
                case God.Zeus:
                    Thunderbolt(target);
                    break;
                case God.Thor:
                    Mjolnir(source, target);
                    break;
                case God.Ra:
                    EyeOfRa();
                    break;
                default:
                    Harvest();
                    break;
            }
        }

        /// <summary>
        /// Marches all the origin's troops but one (at most <see cref="GodPowers.SacredRoadMaxTroops"/>)
        /// along the Sacred Road to a destination of the current god's own. The Road is spent.
        /// </summary>
        public void UseRoad(Territory from, Territory to)
        {
            RequirePhase(TurnPhase.Attack, "march along the Sacred Road");
            RequireTerritory(from, nameof(from));
            RequireTerritory(to, nameof(to));
            if (!CanUseRoad(from, to))
                throw new InvalidOperationException(
                    $"Illegal Sacred Road march {from} -> {to}: it needs an open Road, "
                        + "2+ troops and an own territory at most "
                        + $"{GodPowers.SacredRoadMaxSteps} steps away through own territories."
                );

            God god = CurrentGod;
            List<Territory> path = GodPowers.RoadPath(Board, god, from, to);
            int moved = GodPowers.RoadTroops(from.Troops);
            _roadAvailable = false;
            from.Troops -= moved;
            to.Troops += moved;
            TerritoryChanged?.Invoke(from);
            TerritoryChanged?.Invoke(to);
            RoadUsed?.Invoke(god, from, to, path, moved);
        }

        public void EndTurn()
        {
            RequirePhase(TurnPhase.Attack, "end the turn");

            // Powers bought for this turn end with it.
            _roadAvailable = false;
            _wrathUsedThisTurn = false;
            IsHarvestActive = false;
            _harvestedThisTurn = 0;

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
            DayGod = DayOf(DestinyFace);
            _crowsThisRound = 0;
            RoundStarted?.Invoke(Round, DestinyFace);

            // A god's face and the bad omen exclude each other, so at most one of these runs.
            if (Destiny.IsBadOmen(DestinyFace))
                ApplyBadOmen();
            if (DayGod != null)
                StartDay(DayGod.Value);
        }

        // An eliminated god has no Day: there would be nothing to show for it.
        private God? DayOf(int destinyFace)
        {
            God? god = Pantheon.GodOfFace(destinyFace);
            if (!PowersEnabled || god == null || IsEliminated(god.Value))
                return null;
            return god;
        }

        // Storm and Sun at noon resolve now, like the bad omen; Ragnarök and Night of crows are
        // read during the round's combats.
        private void StartDay(God god)
        {
            switch (god)
            {
                case God.Zeus:
                    List<Territory> struck = GodPowers.StormTargets(Board);
                    DayStarted?.Invoke(god, struck);
                    AddTroops(struck, -GodPowers.StormLoss);
                    GatherStorm(struck.Count * GodPowers.StormLoss);
                    break;
                case God.Ra:
                    List<Territory> lit = Board.TerritoriesOf(God.Ra);
                    DayStarted?.Invoke(god, lit);
                    AddTroops(lit, GodPowers.SunAtNoonBonus);
                    break;
                default:
                    DayStarted?.Invoke(god, Array.Empty<Territory>());
                    break;
            }
        }

        // Zeus draws the lightning back: every troop the Storm struck joins him. A living god always
        // has a destination, because an eliminated god has no Day.
        private void GatherStorm(int troops)
        {
            if (troops == 0)
                return;

            Territory home = GodPowers.StormDestination(Board);
            home.Troops += troops;
            TerritoryChanged?.Invoke(home);
        }

        private void AddTroops(List<Territory> territories, int amount)
        {
            foreach (Territory territory in territories)
            {
                territory.Troops += amount;
                TerritoryChanged?.Invoke(territory);
            }
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
                Territory target = Destiny.BadOmenTarget(Board, god);
                if (target == null)
                    continue;

                target.Troops--;
                TerritoryChanged?.Invoke(target);
            }
        }

        private void StartTurn()
        {
            God god = CurrentGod;
            _roadAvailable = false;

            if (Board.TempleCount(god) >= TemplesToWin)
            {
                Win(god, VictoryReason.Temples);
                return;
            }

            TurnReinforcements = new Reinforcements(
                Board.TerritoryCount(god),
                Board.TempleCount(god),
                DestinyModifierOf(god)
            );
            ReinforcementsLeft = TurnReinforcements.Total;
            Phase = TurnPhase.Reinforce;
            TurnStarted?.Invoke(god, ReinforcementsLeft);
        }

        /// <summary>
        /// What this round's Destiny face does to a god's reinforcements: the favored +2, the rival's
        /// -2, good fortune +1, else 0. On Ra's Day, Sun at noon replaces his +2 (his rival is still
        /// weakened). Public so the UI explains the same number the rules use.
        /// </summary>
        public int DestinyModifierOf(God god)
        {
            if (DayGod == God.Ra && god == God.Ra)
                return 0;
            return Destiny.ReinforcementModifier(god, DestinyFace);
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
            CheckElimination(previousOwner);
        }

        // Shared by captures and the Eye of Ra, the two ways a territory changes hands.
        private void CheckElimination(God? previousOwner)
        {
            if (previousOwner == null || !IsEliminated(previousOwner.Value))
                return;

            // Eliminated nations keep no Favor.
            SetFavor(previousOwner.Value, 0);
            NationEliminated?.Invoke(previousOwner.Value);
            if (LivingNationCount() == 1)
                Win(CurrentGod, VictoryReason.LastStanding);
        }

        // Every combat goes through here so the god powers that change a combat (Ragnarök) and the
        // troop caps apply the same way to the first roll and to an Offering re-roll.
        private CombatResult ResolveCombat(
            Territory from,
            Territory to,
            God defender,
            int attackerRoll,
            int defenderRoll
        )
        {
            CombatResult result = CombatResolver.Resolve(
                CurrentGod,
                attackerRoll,
                defender,
                defenderRoll,
                IsRagnarokActive
            );

            // The defender may lose everything (that is a capture); the attacking territory, like
            // every source, always keeps one troop.
            int canLose = result.Winner == CombatSide.Attacker ? to.Troops : from.Troops - 1;
            return result.WithLoserLosses(Math.Min(result.LoserLosses, canLose));
        }

        // Harvest and Night of crows both turn lost troops into Morrígan's. Each lost troop joins her
        // at most once: her Harvest reaps first (up to its cap), the crows may take what is left.
        private void GatherLostTroops(Combat combat, bool attackerWon, int losses, bool captured)
        {
            int unclaimed = losses;
            if (attackerWon && HarvestTroopsLeft > 0)
            {
                int reaped = Math.Min(unclaimed, HarvestTroopsLeft);
                _harvestedThisTurn += reaped;
                unclaimed -= reaped;
                GiveToMorrigan(combat.From, reaped, captured, combat);
                combat.HarvestTroops = reaped;
            }

            // Morrígan's own losses do not feed her crows.
            God loserGod = attackerWon ? combat.Defender : combat.Attacker;
            if (!IsNightOfCrowsActive || loserGod == God.Morrigan)
                return;

            Territory nest = GodPowers.NightOfCrowsTarget(Board);
            int crows = Math.Min(unclaimed, GodPowers.NightOfCrowsMaxPerRound - _crowsThisRound);
            if (nest == null || crows <= 0)
                return;

            _crowsThisRound += crows;
            GiveToMorrigan(nest, crows, captured, combat);
            combat.CrowTroops = crows;
            combat.CrowTerritory = nest;
        }

        // Added before the capture moves troops, so a captured territory receives them too. When they
        // land on the attacking territory of a capture, Capture reports it, so it is not reported twice.
        private void GiveToMorrigan(Territory territory, int troops, bool captured, Combat combat)
        {
            territory.Troops += troops;
            if (!(captured && territory == combat.From))
                TerritoryChanged?.Invoke(territory);
        }

        // Only the final dice count: an Offering replaces the attacker's die before this runs.
        private void UpdateFavor(Combat combat)
        {
            if (!PowersEnabled)
                return;

            AddFavor(combat.Attacker, FavorDelta(combat.Result.AttackerOutcome));

            // A defender losing its last territory is about to be eliminated and keeps no Favor, so
            // a +1 here would only flash on the meter before the reset.
            bool defenderFalls =
                combat.To.Troops == 0 && Board.TerritoryCount(combat.Defender) == 1;
            if (!defenderFalls)
                AddFavor(combat.Defender, FavorDelta(combat.Result.DefenderOutcome));
        }

        private static int FavorDelta(RollOutcome outcome)
        {
            switch (outcome)
            {
                case RollOutcome.Sacred:
                    return GodPowers.FavorPerOmen;
                case RollOutcome.Cursed:
                    return -GodPowers.FavorPerOmen;
                default:
                    return 0;
            }
        }

        private void AddFavor(God god, int delta) => SetFavor(god, FavorOf(god) + delta);

        // Clamped to the meter; raises FavorChanged only on a real change, so it never fires with powers off.
        internal void SetFavor(God god, int favor)
        {
            int clamped = Math.Max(0, Math.Min(GodPowers.MaxFavor, favor));
            if (_favor[(int)god] == clamped)
                return;

            _favor[(int)god] = clamped;
            FavorChanged?.Invoke(god, clamped);
        }

        // Zeus: half the target's troops, rounded up; never a capture.
        private void Thunderbolt(Territory target)
        {
            WrathUnleashed?.Invoke(God.Zeus, new[] { target });
            target.Troops -= GodPowers.ThunderboltLoss(target.Troops);
            TerritoryChanged?.Invoke(target);
        }

        // Thor: an attack with no roll that takes a fixed blow; a defender left empty is captured
        // exactly like after a won combat.
        private void Mjolnir(Territory from, Territory to)
        {
            WrathUnleashed?.Invoke(God.Thor, new[] { to });
            to.Troops -= GodPowers.MjolnirLoss(to.Troops);
            if (to.Troops == 0)
                Capture(from, to);
            else
                TerritoryChanged?.Invoke(to);
        }

        // Ra: the chosen territories change hands where they stand, keeping their single troop.
        private void EyeOfRa()
        {
            List<Territory> converted = GodPowers.EyeOfRaTargets(Board);
            WrathUnleashed?.Invoke(God.Ra, converted);
            // A conversion that wins the match leaves no enemy territory, so the loop ends by itself.
            foreach (Territory territory in converted)
            {
                God? previousOwner = territory.Owner;
                territory.Owner = God.Ra;
                TerritoryChanged?.Invoke(territory);
                TerritoryCaptured?.Invoke(territory, God.Ra, previousOwner);
                CheckElimination(previousOwner);
            }
        }

        // Morrígan: read by GatherLostTroops for the rest of her turn.
        private void Harvest()
        {
            IsHarvestActive = true;
            WrathUnleashed?.Invoke(God.Morrigan, Array.Empty<Territory>());
        }

        private bool IsEnemyOnBoard(Territory territory) =>
            IsOnBoard(territory) && territory.Owner != null && territory.Owner != CurrentGod;

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
            _roadAvailable = false;
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
