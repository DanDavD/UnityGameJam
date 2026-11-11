using System;
using System.Collections;
using System.Collections.Generic;
using GameJam.Core;
using GameJam.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Turns Match events into what the player sees and hears: board pieces, nations panel, dice,
    /// Destiny Die, turn banner, onboarding hints, audio cues, screen shake and the end screen, plus
    /// the god powers (Day line and badge, Favor meters, Wrath and conversion news, "+1" / "-3" floats).
    /// It never issues Core commands; MatchController owns the Match and the turn loop, attaches
    /// this presenter to the Match, and yields on <see cref="PlayAnnouncements"/> before anyone acts.
    /// Everything personal (identity card, hints, stings, Favor news) speaks to one person at a time:
    /// solo, always the player; hot-seat, the person whose turn it is, or the person a combat
    /// involves (<see cref="PlayerSeats.PerspectiveOf"/>).
    /// </summary>
    public sealed class MatchPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField]
        private BoardView _board;

        [SerializeField]
        private HudView _hud;

        [SerializeField]
        private DiceView _dice;

        [SerializeField]
        private NationsView _nations;

        [SerializeField]
        private TurnBannerView _turnBanner;

        [SerializeField]
        private DestinyView _destiny;

        [Header("Audio and juice")]
        [SerializeField]
        private SfxPlayer _sfx;

        [SerializeField]
        private CameraShake _cameraShake;

        [SerializeField]
        private GodVfx _godVfx;

        [SerializeField]
        private AttackPathView _attackPath;

        [Tooltip(
            "Seconds the attack's march line takes to reach its target; the dice roll meanwhile."
        )]
        [SerializeField, Min(0.05f)]
        private float _attackPathSeconds = 0.35f;

        [Tooltip("Seconds the march line takes to fade out once the combat resolves.")]
        [SerializeField, Min(0.05f)]
        private float _attackPathFadeSeconds = 0.4f;

        [Tooltip(
            "Longest wait for a Day's effect before the turn goes on: a cosmetic effect must never freeze the game."
        )]
        [SerializeField, Min(0f)]
        private float _maxDayVfxSeconds = 4f;

        [Tooltip("Seconds each territory of a Sacred Road march stays lit before the next one.")]
        [SerializeField, Min(0.05f)]
        private float _roadStepSeconds = 0.35f;

        private Match _match;
        private PlayerSeats _seats;

        // A Sacred Road march is lighting its path; the turn loop waits so nothing else plays over it.
        private bool _roadPlaying;

        // When the Favor news of the last sacred die was shown; the Road notice waits RoadNoticeGap after it.
        private const float RoadNoticeGap = 1.3f;
        private float _favorNewsAt = float.NegativeInfinity;

        // The person whose Road just opened and has not seen its popup yet.
        private God? _roadPopupFor;

        // Set on TurnStarted and played by the loop, after the Destiny Die when a round also started.
        private God? _pendingTurnBanner;

        // The HUD round line (and the round counter under it) waits for the Destiny Die to land, so
        // it never spoils the roll.
        private string _pendingRoundLine;
        private string _pendingRoundCounter;

        // Territories the round start changed (bad omen, Storm, Sun at noon) while the Destiny Die
        // was still rolling. Their pieces are redrawn with their "-1" / "+1" after it lands, for the
        // same reason.
        private readonly List<Territory> _roundStartChanges = new List<Territory>();

        // The Day of this round, shown after the Destiny Die like the round line: the badge of a
        // round-long Day, or the news line of an instant one. Null when there is no Day.
        private God? _pendingDayGod;
        private string _pendingDayBadge;
        private string _pendingDayNews;

        // The round info panel's text, built on RoundStarted (before the bad omen hits, so it can name
        // its targets) and DayStarted, and shown with the round line. One line per god, BaseTurnOrder.
        private readonly string[] _roundInfoGodLines = new string[Match.BaseTurnOrder.Count];
        private string _roundInfoTitle;
        private string _roundInfoOpener;
        private string _roundInfoDayHeading;
        private string _roundInfoDay;

        // What the Destiny Die means for the human, played when it lands (null: neutral, no sting).
        private SfxCue? _destinyLandCue;

        // What this round's Destiny face does to the human's reinforcements, kept for the Storm's
        // "You" line, which is only known once the Day starts. Solo only.
        private int _humanDestinyModifier;

        // The person the identity card shows and the hints speak to: solo, the player; hot-seat,
        // the person who played last (it swaps with the banner of each person's turn).
        private God _viewer;

        // Every god's Favor as last shown, indexed like Match.BaseTurnOrder, so a change can say +1 or -1.
        private readonly int[] _shownFavor = new int[Match.BaseTurnOrder.Count];

        // "YOU" or "P1".."P4" per god (null for a bot), shared by the nations panel and identity card.
        private string[] _playerTags;

        // The end banner can show twice (human eliminated, then the last bot wins); the sting plays once.
        private bool _endCuePlayed;

        // A Day started this round; read when the Destiny Die lands.
        private bool _dayThisRound;

        // The Day's effect on the board (Storm bolts, sun rays, embers, crows), played after the
        // Destiny Die lands, with the troop changes it explains. Null when there is none to play.
        private God? _pendingDayVfx;
        private readonly List<Territory> _dayVfxTargets = new List<Territory>();
        private Territory _dayVfxHome;

        // Fast bots shortens a bot's power effects like its other timings; set by the turn loop.
        private float _fastBotFactor = 1f;

        private float VfxPace => MatchSession.FastBots && !IsHumanTurn ? _fastBotFactor : 1f;

        // End-screen stat (combats won per god, indexed like Match.BaseTurnOrder) and one-time
        // onboarding hints, once per match whoever sees them; both live only for this match (scene load).
        private readonly int[] _combatsWon = new int[Match.BaseTurnOrder.Count];
        private readonly HashSet<Hint> _shownHints = new HashSet<Hint>();

        private bool IsHumanTurn => _seats.IsHuman(_match.CurrentGod);

        private void OnEnable()
        {
            _dice.Landed += HandleDiceLanded;
            _destiny.Landed += HandleDestinyLanded;
            // One info card open at a time, across the round line and the nations panel.
            _hud.RoundInfoOpened += _nations.CloseInfo;
            _nations.InfoOpened += _hud.CloseRoundInfo;
        }

        private void OnDisable()
        {
            _dice.Landed -= HandleDiceLanded;
            _destiny.Landed -= HandleDestinyLanded;
            _roadPlaying = false;
            _hud.RoundInfoOpened -= _nations.CloseInfo;
            _nations.InfoOpened -= _hud.CloseRoundInfo;
        }

        // Esc closes any open info card (it also cancels an aimed Wrath in MatchController: both mean
        // "never mind"). Polled here because the UI assembly has no Input System reference.
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;
            _hud.CloseRoundInfo();
            _nations.CloseInfo();
        }

        /// <summary>
        /// Subscribes to the match. Called by MatchController, which owns the Match, as soon as the
        /// Match exists (after the menu), so the subscription is in place before Match.Start.
        /// </summary>
        public void Attach(Match match)
        {
            _match = match;
            _match.RoundStarted += HandleRoundStarted;
            _match.TurnStarted += HandleTurnStarted;
            _match.TerritoryChanged += HandleTerritoryChanged;
            _match.TerritoryCaptured += HandleTerritoryCaptured;
            _match.CombatRolled += HandleCombatRolled;
            _match.CombatResolved += HandleCombatResolved;
            _match.NationEliminated += HandleNationEliminated;
            _match.MatchWon += HandleMatchWon;
            _match.DayStarted += HandleDayStarted;
            _match.FavorChanged += HandleFavorChanged;
            _match.WrathUnleashed += HandleWrathUnleashed;
            _match.RoadOpened += HandleRoadOpened;
            _match.RoadUsed += HandleRoadUsed;
            _hud.ShowFavor(match.PowersEnabled);
            _nations.ShowPowers(match.PowersEnabled);
        }

        /// <summary>Unsubscribes from the match; called from MatchController.OnDisable.</summary>
        public void Detach()
        {
            _match.RoundStarted -= HandleRoundStarted;
            _match.TurnStarted -= HandleTurnStarted;
            _match.TerritoryChanged -= HandleTerritoryChanged;
            _match.TerritoryCaptured -= HandleTerritoryCaptured;
            _match.CombatRolled -= HandleCombatRolled;
            _match.CombatResolved -= HandleCombatResolved;
            _match.NationEliminated -= HandleNationEliminated;
            _match.MatchWon -= HandleMatchWon;
            _match.DayStarted -= HandleDayStarted;
            _match.FavorChanged -= HandleFavorChanged;
            _match.WrathUnleashed -= HandleWrathUnleashed;
            _match.RoadOpened -= HandleRoadOpened;
            _match.RoadUsed -= HandleRoadUsed;
        }

        /// <summary>Shows who plays which god, once the seats are chosen and before the match starts.</summary>
        public void SetSeats(PlayerSeats seats)
        {
            _seats = seats;
            _playerTags = PlayerTags(seats);
            _nations.SetPlayerTags(_playerTags);
            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                God god = Match.BaseTurnOrder[i];
                _shownFavor[i] = _match.FavorOf(god);
                // Favor is only redrawn on FavorChanged after this, so a meter can wait for its star.
                _nations.SetFavor(god, _shownFavor[i]);
            }
            ShowIdentity(seats.FirstPlayer);
            _nations.Refresh(_match);
        }

        /// <summary>Sorts the nations panel into the drawn turn order, for the whole match.</summary>
        public void ShowTurnOrder(IReadOnlyList<God> order) => _nations.SetTurnOrder(order);

        // Solo: "YOU" on the player. Hot-seat: "P1".."P4" in base turn order, since "YOU" would be
        // wrong for everyone but the person holding the mouse.
        private static string[] PlayerTags(PlayerSeats seats)
        {
            var tags = new string[Match.BaseTurnOrder.Count];
            int number = 0;
            for (int i = 0; i < tags.Length; i++)
            {
                if (!seats.IsPlayerSeat(Match.BaseTurnOrder[i]))
                    continue;
                number++;
                tags[i] = seats.IsHotSeat ? MatchText.PlayerTag(number) : MatchText.YouTag;
            }
            return tags;
        }

        // The identity card (chip, name, sacred / cursed, Wrath stars) for this person.
        private void ShowIdentity(God god)
        {
            _viewer = god;
            _hud.SetIdentity(god, _playerTags[Match.BaseTurnIndex(god)]);
            RefreshIdentityFavor();
        }

        /// <summary>
        /// The Destiny Die (when a round just started), then the turn banner, before anyone acts.
        /// </summary>
        /// <param name="fastBotFactor">Shortens a bot's turn banner while fast bots is on.</param>
        public IEnumerator PlayAnnouncements(float fastBotFactor)
        {
            while (_destiny.IsPlaying || _roadPlaying)
                yield return null;
            _fastBotFactor = fastBotFactor;
            // Read after the wait, so toggling fast bots during the Destiny Die still applies.
            float botPaceFactor = MatchSession.FastBots ? fastBotFactor : 1f;
            bool roundStarted = _pendingRoundLine != null;
            ApplyPendingRoundLine();
            if (roundStarted)
            {
                // The Day's effect plays alone, before the turn banner (one strong animation at a
                // time), and before its result: the "-1" / "+1" and the news line follow the effect.
                // Sped up only when the Day belongs to a bot, like the rest of a bot's timings.
                if (_pendingDayVfx.HasValue)
                {
                    God dayGod = _pendingDayVfx.Value;
                    PlayPendingDayVfx(_seats.IsHuman(dayGod) ? 1f : botPaceFactor);
                    // Capped: a broken cosmetic effect must never freeze the turn loop.
                    float waited = 0f;
                    while (_godVfx.IsPlaying && waited < _maxDayVfxSeconds)
                    {
                        waited += Time.deltaTime;
                        yield return null;
                    }
                }
                ShowRoundStartChanges();
                ShowPendingDayNews();
                // A skipped Destiny Die never lands, so its roll would otherwise keep rattling.
                _sfx.StopRoll();
                // After the die, not under its overlay, so the hint is readable when it explains the roll.
                // The Destiny hint comes first; the Day hint waits for the next Day if both are new.
                if (!ShowHintOnce(Hint.Destiny) && _match.DayGod != null)
                    ShowHintOnce(Hint.Day);
            }

            if (_pendingTurnBanner != null)
                yield return PlayTurnBanner(botPaceFactor);
            RefreshReinforceCounter();
        }

        /// <summary>
        /// The march line of an attack, drawn from <paramref name="from"/> to <paramref name="to"/> as
        /// the attack starts. Called by MatchController just before the attack command, for the person
        /// and for the bots alike. It stays lit through the roll and fades when the combat resolves.
        /// </summary>
        public void ShowAttackPath(Territory from, Territory to)
        {
            // An empty territory is taken with no dice: its path fades by itself.
            _attackPath.Play(
                from,
                to,
                _match.CurrentGod,
                _attackPathSeconds * VfxPace,
                fadeByItself: to.Owner == null
            );
        }

        private IEnumerator PlayTurnBanner(float botPaceFactor)
        {
            God god = _pendingTurnBanner.Value;
            _pendingTurnBanner = null;
            // The turn highlight moves with the banner, not before the Destiny Die has landed.
            _nations.Refresh(_match);
            bool human = IsHumanTurn;
            // The turn box is the persistent indicator, so it shows even when the slide-in banner is skipped.
            _hud.ShowTurn(
                god,
                MatchText.TurnBox(
                    god,
                    human,
                    _seats.IsHotSeat,
                    _playerTags[Match.BaseTurnIndex(god)]
                )
            );
            if (human)
            {
                // "YOUR TURN" is the moment to act: the board top must be clear. In hot-seat the
                // identity card swaps to this person with the banner that asks for the mouse.
                _hud.CloseRoundInfo();
                // A hint worded for the previous person (their numbers) must not carry over, nor
                // hold back this person's Reinforce hint.
                if (god != _viewer)
                    _hud.HideHint();
                ShowIdentity(god);
            }
            else if (_hud.IsRoundInfoOpen)
            {
                // The player is reading the round info, which the banner would cover. A bot's turn
                // is still shown by the nations panel mark and the "Thor is playing…" prompt.
                yield break;
            }

            _turnBanner.Show(
                god,
                MatchText.TurnBanner(god, human, _seats.IsHotSeat),
                human ? 1f : botPaceFactor
            );
            // Only a person's turn is announced by sound; bot turns change often and stay silent.
            if (human)
                _sfx.Play(SfxCue.TurnChange);
            while (_turnBanner.IsPlaying)
                yield return null;
        }

        // The big "troops to place" counter: only while the human places reinforcements.
        private void RefreshReinforceCounter()
        {
            bool reinforcing =
                _match.Phase == TurnPhase.Reinforce && IsHumanTurn && _match.ReinforcementsLeft > 0;
            if (reinforcing)
                _hud.ShowReinforcements(
                    _match.ReinforcementsLeft,
                    MatchText.ReinforceFormula(_match.TurnReinforcements)
                );
            else
                _hud.HideReinforcements();
        }

        // The first combat a person takes part in teaches its numbers, read through that person's
        // sacred and cursed faces. The first time the attacker can make an Offering, that hint goes
        // first and the numbers wait for the next combat.
        public void ShowCombatHint(bool humanCanOffer)
        {
            Combat combat = _match.PendingCombat;
            God? perspective = _seats.PerspectiveOf(combat.Attacker, combat.Defender);
            if (perspective == null)
                return;
            if (humanCanOffer && ShowHintOnce(Hint.Offering))
                return;
            ShowHintOnce(Hint.Combat, perspective.Value);
        }

        /// <summary>
        /// Shows the hint the first time its situation happens in this match (once for the whole
        /// table in hot-seat), worded for the person on the identity card; false afterwards.
        /// </summary>
        public bool ShowHintOnce(Hint hint) => ShowHintOnce(hint, _viewer);

        private bool ShowHintOnce(Hint hint, God about)
        {
            // The prompt bar already says what these two hints would: never both.
            if (hint == Hint.Reinforce || hint == Hint.Attack)
                return false;
            if (!_seats.HasHuman || !_shownHints.Add(hint))
                return false;
            _hud.ShowHint(MatchText.HintText(hint, about));
            return true;
        }

        private void ShowRoundStartChanges()
        {
            if (_roundStartChanges.Count == 0)
                return;
            foreach (Territory territory in _roundStartChanges)
                _board.RefreshShowingChange(territory);
            _roundStartChanges.Clear();
            _nations.Refresh(_match);
        }

        private void ApplyPendingRoundLine()
        {
            if (_pendingRoundLine == null)
                return;
            _hud.SetRound(_pendingRoundLine, _pendingRoundCounter);
            _pendingRoundLine = null;
            _hud.SetRoundInfo(
                _roundInfoTitle,
                _roundInfoGodLines,
                _roundInfoOpener,
                _roundInfoDayHeading,
                _roundInfoDay
            );

            _hud.SetDayBadge(_pendingDayBadge, _pendingDayGod);
            _pendingDayBadge = null;
        }

        // The news line of an instant Day (Storm, Sun at noon), after its effect has played.
        private void ShowPendingDayNews()
        {
            if (_pendingDayNews != null)
                _hud.ShowNews(_pendingDayNews, _pendingDayGod.Value);
            _pendingDayGod = null;
            _pendingDayNews = null;
        }

        private void HandleRoundStarted(int round, int destinyFace)
        {
            // Match already holds the round's Day here, so its modifiers are final (Sun at noon
            // replaces Ra's +2); the text only says so when that happened.
            God? favored = Pantheon.GodOfFace(destinyFace);
            bool dayReplacesFavor =
                favored.HasValue && _match.DestinyModifierOf(favored.Value) < Destiny.FavoredBonus;
            _pendingRoundLine = MatchText.RoundLine(destinyFace, dayReplacesFavor);
            _pendingRoundCounter = MatchText.RoundCounter(round);
            KeepRoundInfo(destinyFace);
            string destiny = MatchText.DestinyLine(destinyFace, dayReplacesFavor);
            // The sound starts here and the die tumbles in the same call below: same frame, and the
            // tumble lasts exactly as long as the sound.
            float tumble = _sfx.PlayRoll(_destiny.FallbackTumbleDuration);
            // Hot-seat: the die speaks to the whole table, so it has no "You" line and no personal
            // sting (a Day still rings softly); the round info panel gives every god its line.
            if (_seats.IsHotSeat)
            {
                _destinyLandCue = null;
                _destiny.Show(round, destinyFace, destiny, null, null, tumble);
            }
            else
            {
                ShowSoloDestiny(round, destinyFace, destiny, tumble);
            }
            // The last round's Day is over; this round's (if any) follows in DayStarted.
            _dayThisRound = false;
            _pendingDayVfx = null;
            _pendingDayGod = null;
            _pendingDayNews = null;
            _hud.SetDayBadge(null, null);
            // Last round's explanation is stale, and the Destiny Die covers the screen anyway.
            _hud.CloseRoundInfo();
        }

        // Solo: the "You" line and the landing sting say what the face means for the player.
        private void ShowSoloDestiny(int round, int destinyFace, string destiny, float tumble)
        {
            God human = _viewer;
            bool yourDay = _match.DayGod == human;
            // Read before the bad omen is applied (it follows this event), so ask Core who it will hit.
            int modifier = _match.DestinyModifierOf(human);
            _humanDestinyModifier = modifier;
            bool humanLosesTroop =
                Destiny.IsBadOmen(destinyFace)
                && Destiny.BadOmenTarget(_match.Board, human) != null;
            _destiny.Show(
                round,
                destinyFace,
                destiny,
                human,
                MatchText.DestinyYouLine(modifier, humanLosesTroop, yourDay ? human : (God?)null),
                tumble
            );
            // The human's own Day is good news even when it replaces the +2.
            _destinyLandCue = yourDay
                ? SfxCue.Sacred
                : SfxCueChooser.DestinyLanding(modifier, humanLosesTroop);
        }

        // Read now, before the bad omen is applied (it follows RoundStarted): the per-god lines can
        // then name the territory it will hit. The Day block is filled in by DayStarted, if any.
        private void KeepRoundInfo(int destinyFace)
        {
            _roundInfoTitle = MatchText.RoundInfoTitle(destinyFace);
            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
                _roundInfoGodLines[i] = MatchText.RoundInfoGodLine(
                    _match,
                    Match.BaseTurnOrder[i],
                    destinyFace
                );
            _roundInfoOpener = MatchText.RoundInfoOpener(_match, destinyFace);
            _roundInfoDayHeading = null;
            _roundInfoDay = null;
        }

        // Raised right after RoundStarted, while the Destiny Die is rolling, so everything here is
        // revealed when it lands: the Day line on the die, then the badge or news line on the HUD.
        // The troops it changes are held back by HandleTerritoryChanged like the bad omen's.
        private void HandleDayStarted(God god, IReadOnlyList<Territory> changed)
        {
            _dayThisRound = true;
            _pendingDayGod = god;
            KeepDayVfx(god, changed);
            _roundInfoDayHeading = MatchText.RoundInfoDayHeading(god);
            _roundInfoDay = MatchText.RoundInfoDay(god, changed.Count);
            _destiny.SetDayLine(MatchText.DayLine(god), god);
            if (_match.IsRagnarokActive || _match.IsNightOfCrowsActive)
                _pendingDayBadge = MatchText.DayBadge(god);
            else
                _pendingDayNews = MatchText.DayResultLine(god, changed.Count);

            // A Day only ever strikes enemies of its god (Storm), so human territories in the list
            // are troops the human is about to lose: the "You" line and the landing cue say so (solo
            // only, like the rest of the "You" line). The troops it sends to the god's own
            // territories arrive while the die rolls and float "+N" with the rest when it lands.
            if (_seats.IsHotSeat)
                return;
            int humanStruck = god == _viewer ? 0 : CountOwnedBy(changed, _viewer);
            if (humanStruck == 0)
                return;
            _destiny.SetYouLine(
                MatchText.DestinyYouStormLine(
                    _humanDestinyModifier,
                    humanStruck * GodPowers.StormLoss
                )
            );
            if (_destinyLandCue == null)
                _destinyLandCue = SfxCue.Cursed;
        }

        // Copied now: the list belongs to Match, and the Storm's home is read before its troops move.
        private void KeepDayVfx(God god, IReadOnlyList<Territory> changed)
        {
            _pendingDayVfx = god;
            _dayVfxTargets.Clear();
            for (int i = 0; i < changed.Count; i++)
                _dayVfxTargets.Add(changed[i]);
            _dayVfxHome = god switch
            {
                God.Zeus => GodPowers.StormDestination(_match.Board),
                God.Morrigan => GodPowers.NightOfCrowsTarget(_match.Board),
                _ => null,
            };
        }

        // Each Day shows its own shape on the board: Storm bolts whose troops fly to Zeus, sun rays
        // on Ra's land, embers on Thor's pieces, crows coming home to Morrígan's nest.
        private void PlayPendingDayVfx(float pace)
        {
            if (_pendingDayVfx == null)
                return;
            God god = _pendingDayVfx.Value;
            _pendingDayVfx = null;
            switch (god)
            {
                case God.Zeus:
                    _godVfx.PlayStorm(_dayVfxTargets, _dayVfxHome, pace);
                    break;
                case God.Ra:
                    _godVfx.PlaySunAtNoon(_dayVfxTargets, pace);
                    break;
                case God.Thor:
                    _godVfx.PlayEmbers(_match.Board.TerritoriesOf(God.Thor), pace);
                    break;
                default:
                    _godVfx.PlayNightOfCrows(_dayVfxHome, pace);
                    break;
            }
        }

        // The roll is not stopped here: its clip ended with the tumble, on the landing clack. A Day that means
        // nothing for the human still rings, softly, so the god's face is heard as an event.
        private void HandleDestinyLanded()
        {
            if (_destinyLandCue.HasValue)
                _sfx.Play(_destinyLandCue.Value);
            else if (_dayThisRound)
                _sfx.PlaySubtle(SfxCue.Sacred);
        }

        private void HandleTurnStarted(God god, int reinforcements)
        {
            _pendingTurnBanner = god;
            // An unused Road is lost with the turn, and so is its popup.
            _roadPopupFor = null;
            _board.ClearHighlights();
            _attackPath.Clear();
            // The turn box follows at once (hidden under the Destiny Die anyway), never showing the last
            // god's title and status.
            _hud.ShowTurn(
                god,
                MatchText.TurnBox(
                    god,
                    IsHumanTurn,
                    _seats.IsHotSeat,
                    _playerTags[Match.BaseTurnIndex(god)]
                )
            );
            // The last combat stays readable until the next roll or the next turn.
            _dice.Hide();
            if (IsHumanTurn)
            {
                // Every prompt of this turn names whose it is ("ZEUS'S TURN" in hot-seat). Cleared to
                // just that, so the previous bot's "X is playing…" never lingers into it.
                _hud.SetTurnPrefix(MatchText.TurnPrefix(god, _seats.IsHotSeat), god);
                _hud.SetPrompt(string.Empty);
                return;
            }
            // A bot's turn has no prompt bar: the turn box says whose turn it is and what it is doing.
            _hud.SetTurnPrefix(null);
            _hud.SetPrompt(string.Empty);
            // A human hint would otherwise hang over the bot's turn.
            _hud.HideHint();
        }

        private void HandleTerritoryChanged(Territory territory)
        {
            // Only the round start (bad omen, Storm, Sun at noon) changes troops while the Destiny Die
            // rolls; showing it now would spoil the roll.
            if (_destiny.IsPlaying)
            {
                _roundStartChanges.Add(territory);
                return;
            }
            _board.Refresh(territory);
            _nations.Refresh(_match);
            // Each placed troop changes a territory, so the counter counts down with it. Not before
            // "YOUR TURN": PlayAnnouncements shows the counter once the banner is gone.
            if (_pendingTurnBanner == null)
                RefreshReinforceCounter();
        }

        // Taking land from a rival is the strongest board moment, so only it shakes the screen; an
        // empty territory just changes color with its ring.
        private void HandleTerritoryCaptured(Territory territory, God newOwner, God? previousOwner)
        {
            _board.ShowCaptureRing(territory);
            if (previousOwner.HasValue)
                _cameraShake.Shake();
            bool temple = territory.Kind == TerritoryKind.Temple;
            if (temple)
                ShowHintOnce(Hint.Temple);

            // Fired before NationEliminated and MatchWon: when this capture ends the match for the
            // human, the end sting that follows plays alone.
            if (previousOwner.HasValue && _seats.EndsMatchForHumans(_match, previousOwner.Value))
                return;
            _sfx.Play(SfxCue.Capture);
            // A temple is the goal, so its capture carries a quiet chime under the capture cue.
            if (temple)
                _sfx.PlaySubtle(SfxCue.Sacred);
        }

        private void HandleCombatRolled(Combat combat)
        {
            _hud.SetTurnStatus(MatchText.StatusRolling);
            // The re-roll after an Offering is the moment its troop is paid.
            if (combat.OfferingMade)
                _board.ShowTroopChange(combat.From, -Match.OfferingCost);
            // One roll sound for every roll, the Offering re-roll included. It starts here and the
            // dice tumble in the same call: same frame, and the tumble lasts exactly as long as the sound.
            float tumble = _sfx.PlayRoll(_dice.FallbackTumbleDuration);
            _dice.Show(combat, tumble);
        }

        // The roll is not stopped here: its clip ended with the tumble, on the landing clack.
        private void HandleDiceLanded(CombatResult result)
        {
            // The dice of the pending combat; null if the match ended while they were still tumbling.
            Combat combat = _match.PendingCombat;
            if (combat == null)
                return;
            _hud.SetTurnStatus(MatchText.StatusResult);
            SfxCue? cue = SfxCueChooser.CombatLanding(
                combat,
                result,
                _seats.PerspectiveOf(combat.Attacker, combat.Defender)
            );
            if (cue.HasValue)
                _sfx.Play(cue.Value);
        }

        // The dice panel is not hidden here, so the verdict stays readable after the troop is lost.
        private void HandleCombatResolved(Combat combat)
        {
            // The march line goes out with the combat: the result has been read.
            _attackPath.Fade(_attackPathFadeSeconds);
            // Back from "Result". A bot goes back to thinking; a person's status is set by the attack
            // loop (CombatResolved fires after the result window, so "Result" stayed while it was read).
            if (!IsHumanTurn)
                _hud.SetTurnStatus(MatchText.StatusThinking);
            bool attackerWon = combat.Result.Winner == CombatSide.Attacker;
            _combatsWon[Match.BaseTurnIndex(attackerWon ? combat.Attacker : combat.Defender)]++;

            // A defender at 0 is about to be captured: the change of owner already says it lost,
            // and a "-1" over the attacker's freshly moved-in troops would read as a loss.
            bool captured = attackerWon && combat.To.Troops == 0;
            if (!captured)
                _board.ShowTroopChange(
                    attackerWon ? combat.To : combat.From,
                    -combat.Result.LoserLosses
                );
            ShowRagnarokBlow(combat, attackerWon, captured);
            ShowGatheredTroops(combat, captured);
        }

        // A Ragnarök win that costs the loser 2 glows on Thor's piece, so the "-2" has a visible cause.
        private void ShowRagnarokBlow(Combat combat, bool attackerWon, bool captured)
        {
            God winner = attackerWon ? combat.Attacker : combat.Defender;
            if (winner != God.Thor || combat.Result.LoserLosses < GodPowers.RagnarokLosses)
                return;
            // After a capture Thor's troops stand on the taken territory.
            Territory thorPiece = attackerWon ? (captured ? combat.To : combat.From) : combat.To;
            _godVfx.PlayEmbers(thorPiece, VfxPace);
        }

        // Harvest and Night of crows turn a loss into Morrígan's troops: a "+1" where they land and
        // a news line that names the power, so the extra troop never looks like a bug.
        private void ShowGatheredTroops(Combat combat, bool captured)
        {
            if (combat.HarvestTroops > 0)
            {
                // Crows carry the reaped troops from the defender to where they now stand.
                _godVfx.PlayCrows(
                    combat.To,
                    captured ? combat.To : combat.From,
                    combat.HarvestTroops,
                    VfxPace
                );
                // On a capture the reaped troops moved on with the rest; the capture ring shows that.
                if (!captured)
                    _board.ShowTroopChange(combat.From, combat.HarvestTroops);
                _hud.ShowNews(MatchText.HarvestGainLine(combat.HarvestTroops), God.Morrigan);
            }
            if (combat.CrowTroops > 0)
            {
                bool attackerLost = combat.Result.Winner != CombatSide.Attacker;
                Territory nest =
                    captured && combat.CrowTerritory == combat.From
                        ? combat.To
                        : combat.CrowTerritory;
                _godVfx.PlayCrows(
                    attackerLost ? combat.From : combat.To,
                    nest,
                    combat.CrowTroops,
                    VfxPace
                );
                // Same as the Harvest: troops that joined a territory that then captured moved on.
                if (!(captured && combat.CrowTerritory == combat.From))
                    _board.ShowTroopChange(combat.CrowTerritory, combat.CrowTroops);
                _hud.ShowNews(MatchText.CrowGainLine(combat.CrowTroops), God.Morrigan);
            }
        }

        private void HandleFavorChanged(God god, int favor)
        {
            int index = Match.BaseTurnIndex(god);
            int delta = favor - _shownFavor[index];
            _shownFavor[index] = favor;
            ShowFavorFromDie(god, favor, delta);

            // The news speaks to the person the combat is read through (see PerspectiveOf): the
            // attacker on their own turn, else a person defending. Bots (and the debug bot seat)
            // only bump their meter.
            if (_seats.PerspectiveOf(_match.CurrentGod, god) != god)
                return;
            // Only a combat die moves Favor by one step; a Wrath or an elimination empties it and
            // has its own news, so only the one-step changes of a nation still in play are explained.
            if (Math.Abs(delta) != GodPowers.FavorPerOmen || _match.IsEliminated(god))
                return;
            _favorNewsAt = Time.time;
            _hud.ShowNews(
                MatchText.FavorChangeLine(god, delta, favor),
                god,
                delta > 0 ? RollOutcome.Sacred : RollOutcome.Cursed
            );
            ShowHintOnce(Hint.Favor, god);
        }

        // A one-step change comes from a combat die still on the table (Match.UpdateFavor, raised
        // before CombatResolved): a sacred die sends a star to the god's stars, which fill as it
        // lands; a cursed one cracks a star where it is. The person on the identity card sees their
        // own stars there, everyone else's in the nations panel. A Wrath or an elimination empties
        // the meter at once, with its own news.
        private void ShowFavorFromDie(God god, int favor, int delta)
        {
            Combat combat = _match.PendingCombat;
            bool fromDie =
                Math.Abs(delta) == GodPowers.FavorPerOmen
                && !_match.IsEliminated(god)
                && combat != null
                && (combat.Attacker == god || combat.Defender == god)
                && _dice.IsShown;
            bool toIdentity = god == _viewer && _hud.IsIdentityShown;
            UnityEngine.UIElements.VisualElement meter = toIdentity
                ? _hud.IdentityFavor
                : _nations.FavorMeter(god);
            if (fromDie && delta > 0)
            {
                CombatSide side =
                    combat.Attacker == god ? CombatSide.Attacker : CombatSide.Defender;
                _hud.PlayFavorStar(
                    _dice.DieOf(side),
                    meter,
                    god,
                    VfxPace,
                    () => RedrawFavor(god, favor)
                );
                return;
            }

            RedrawFavor(god, favor);
            if (fromDie)
                _hud.PlayFavorCrack(meter, VfxPace);
        }

        private void RedrawFavor(God god, int favor)
        {
            _nations.ShowFavorChanged(god, favor);
            if (god == _viewer)
                RefreshIdentityFavor();
        }

        // The identity card's Wrath line follows Favor, so it changes on FavorChanged and when the
        // card swaps to another person.
        private void RefreshIdentityFavor() => _hud.SetFavor(_viewer, _match.FavorOf(_viewer));

        // Raised before the effect, so the losses are read from the troops still on the board.
        // Captures and conversions follow as TerritoryCaptured, which brings its own ring and cue.
        private void HandleWrathUnleashed(God god, IReadOnlyList<Territory> targets)
        {
            _board.ClearHighlights();
            string news;
            switch (god)
            {
                case God.Zeus:
                {
                    Territory target = targets[0];
                    int loss = GodPowers.ThunderboltLoss(target.Troops);
                    _godVfx.PlayThunderbolt(target, VfxPace);
                    ShowWrathBlow(target, loss);
                    news = MatchText.ThunderboltLine(target, loss);
                    break;
                }
                case God.Thor:
                {
                    Territory target = targets[0];
                    int loss = GodPowers.MjolnirLoss(target.Troops);
                    bool captures = loss == target.Troops;
                    _godVfx.PlayMjolnir(target, VfxPace);
                    if (!captures)
                        ShowWrathBlow(target, loss);
                    news = MatchText.MjolnirLine(target, loss, captures);
                    break;
                }
                case God.Ra:
                    for (int i = 0; i < targets.Count; i++)
                        _godVfx.PlayEyeOfRa(targets[i], VfxPace);
                    news = MatchText.EyeOfRaLine(targets);
                    break;
                default:
                    news = MatchText.HarvestLine;
                    break;
            }
            // The god answers with the sacred chime, the sound of its Favor.
            _sfx.PlaySubtle(SfxCue.Sacred);
            _hud.ShowNews(news, god);
        }

        // The gods open the road. A bot gets a discreet news line with the sacred icon. A person gets
        // the popup instead, but only once MatchController knows an army can march, so it is kept
        // for ShowRoadPopupIfOpened. Both wait for the Favor line of the same die to be read first.
        private void HandleRoadOpened(God god)
        {
            if (_seats.IsHuman(god))
            {
                _roadPopupFor = god;
                return;
            }
            StartCoroutine(ShowRoadNoticeAfter(MatchText.RoadOpenedLine(god), god, RoadNoticeWait));
        }

        // How long until the same sacred die's Favor line has had RoadNoticeGap to be read.
        private float RoadNoticeWait => _favorNewsAt + RoadNoticeGap * VfxPace - Time.time;

        private IEnumerator ShowRoadNoticeAfter(string line, God god, float wait)
        {
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
            // The chime comes with the line, not with the die.
            _sfx.PlaySubtle(SfxCue.Sacred);
            _hud.ShowNews(line, god, RollOutcome.Sacred);
        }

        /// <summary>
        /// The first time a person's Road opens and an army can march: waits out the Favor line, then
        /// slides in the popup with the sacred chime. Does nothing when the popup was already shown
        /// (the loop came back to the attack loop after a Wrath), so MatchController draws the rings
        /// right after this returns, together with the popup.
        /// </summary>
        public IEnumerator ShowRoadPopupIfOpened()
        {
            if (!_roadPopupFor.HasValue)
                yield break;
            God god = _roadPopupFor.Value;
            _roadPopupFor = null;
            float wait = RoadNoticeWait;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
            bool you = !_seats.IsHotSeat;
            _sfx.PlaySubtle(SfxCue.Sacred);
            _turnBanner.ShowRoadPopup(god, MatchText.RoadPopupText(god, you));
        }

        /// <summary>Drops a Road popup that never got shown (no army could march), so it never appears later unrelated to a die.</summary>
        public void ClearPendingRoadPopup() => _roadPopupFor = null;

        /// <summary>Closes the Sacred Road popup when the person acts on it (or a new turn starts).</summary>
        public void HideRoadPopup() => _turnBanner.HideRoadPopup();

        private void HandleRoadUsed(
            God god,
            Territory from,
            Territory to,
            IReadOnlyList<Territory> path,
            int troops
        )
        {
            _board.ClearHighlights();
            _hud.ShowNews(MatchText.RoadUsedLine(god, from, to, troops), god);
            StartCoroutine(PlayRoad(from, to, path, troops, VfxPace));
        }

        // One strong animation: the path lights up territory by territory in the god's color (the
        // capture ring), the origin floats "-N" as the army leaves and the destination "+N" as it arrives.
        // The pieces already hold the new counts (Match raised TerritoryChanged first); only the floats wait.
        private IEnumerator PlayRoad(
            Territory from,
            Territory to,
            IReadOnlyList<Territory> path,
            int troops,
            float pace
        )
        {
            _roadPlaying = true;
            float step = _roadStepSeconds * pace;
            for (int i = 0; i < path.Count; i++)
            {
                // The ring first: it waits for a float still on its piece, so the float starts after.
                _board.ShowCaptureRing(path[i]);
                if (path[i] == from)
                    _board.ShowTroopChange(from, -troops);
                if (path[i] == to)
                {
                    _board.ShowTroopChange(to, troops);
                    _sfx.Play(SfxCue.TroopPlaced);
                }
                for (float elapsed = 0f; elapsed < step; elapsed += Time.deltaTime)
                    yield return null;
            }
            _roadPlaying = false;
        }

        // Thunderbolt and a Mjölnir that does not capture hit like a capture: the strongest board moment.
        private void ShowWrathBlow(Territory target, int loss)
        {
            _board.ShowTroopChange(target, -loss);
            _cameraShake.Shake();
            _sfx.Play(SfxCue.Capture);
        }

        private static int CountOwnedBy(IReadOnlyList<Territory> territories, God god)
        {
            int count = 0;
            for (int i = 0; i < territories.Count; i++)
            {
                if (territories[i].Owner == god)
                    count++;
            }
            return count;
        }

        // The turn loop stops on its own once every person is out (PlayerSeats.AreAllHumansOut);
        // this only shows it.
        private void HandleNationEliminated(God god)
        {
            _nations.Refresh(_match);
            if (_seats.EndsMatchForHumans(_match, god))
            {
                // No winner yet: the bots could keep playing, so the banner stays neutral. When one
                // nation is left instead, MatchWon follows and its end sting plays alone.
                if (_seats.AreAllHumansOut(_match))
                    ShowEndBanner(
                        _seats.IsHotSeat ? MatchText.AllPlayersOutTitle : MatchText.LoseTitle,
                        _seats.IsHotSeat
                            ? MatchText.AllPlayersOutLine
                            : MatchText.EliminatedLine(god),
                        null
                    );
                return;
            }

            // A nation falling is news, not the end: a quiet defeat cue. In hot-seat a person can
            // fall while the others play on, so the news line says so.
            _sfx.PlaySubtle(SfxCue.Defeat);
            if (!_seats.IsHuman(god))
                return;
            _hud.ShowNews(MatchText.PlayerOutLine(god), god);
            // The card would speak for someone who is out; the next person's banner brings it back.
            if (god == _viewer)
                _hud.HideIdentity();
        }

        // When the last person's elimination leaves one nation, this follows HandleNationEliminated
        // and the banner names that winner instead. Solo it is the player's verdict (YOU WIN /
        // YOU LOSE); hot-seat speaks to the whole table (THOR WINS).
        private void HandleMatchWon(God winner, VictoryReason reason) =>
            ShowEndBanner(
                _seats.IsHotSeat
                    ? MatchText.WinsTitle(winner)
                    : MatchText.EndTitle(winner == _seats.FirstPlayer),
                MatchText.VictoryLine(reason, _match.RoundLimitDecider),
                winner
            );

        private void ShowEndBanner(string title, string detail, God? winner)
        {
            // A temple win at a round's first turn ends the match while the Destiny Die still rolls.
            _destiny.Hide();
            ShowRoundStartChanges();
            _board.ClearHighlights();
            _attackPath.Clear();
            _nations.Refresh(_match);
            // A temple win at a round's first turn ends the match before the loop shows the line.
            ApplyPendingRoundLine();
            _hud.SetDayBadge(null, null);
            _hud.ShowEndTurn(false);
            _hud.SetTurnPrefix(null);
            _hud.SetPrompt(string.Empty);
            // A person won (solo: the player; hot-seat: any of them) or not.
            PlayEndCueOnce(winner.HasValue && _seats.IsPlayerSeat(winner.Value));
            int winnerTemples = winner.HasValue ? _match.Board.TempleCount(winner.Value) : 0;
            // Hot-seat's title already says "THOR WINS", so the card only names the god.
            string winnerName = winner.HasValue
                ? _seats.IsHotSeat
                    ? MatchText.GodName(winner.Value)
                    : MatchText.WinnerName(winner.Value)
                : null;
            _hud.ShowEndBanner(
                title,
                detail,
                winner,
                winnerName,
                _match.Round,
                winnerTemples,
                CombatsWonStat(winner, out string combatsLabel),
                combatsLabel
            );
        }

        // Solo: the player's combats. Hot-seat: the winner's, or every person's when nobody won.
        private int CombatsWonStat(God? winner, out string label)
        {
            if (!_seats.IsHotSeat)
            {
                label = MatchText.CombatsWonStat;
                return _combatsWon[Match.BaseTurnIndex(_seats.FirstPlayer)];
            }
            if (winner.HasValue)
            {
                label = MatchText.CombatsWonByStat(winner.Value);
                return _combatsWon[Match.BaseTurnIndex(winner.Value)];
            }

            label = MatchText.CombatsWonByPlayersStat;
            int total = 0;
            for (int i = 0; i < _combatsWon.Length; i++)
            {
                if (_seats.IsPlayerSeat(Match.BaseTurnOrder[i]))
                    total += _combatsWon[i];
            }
            return total;
        }

        private void PlayEndCueOnce(bool humanWon)
        {
            if (_endCuePlayed)
                return;
            _endCuePlayed = true;
            _sfx.StopRoll();
            _sfx.Play(humanWon ? SfxCue.Victory : SfxCue.Defeat);
        }
    }
}
