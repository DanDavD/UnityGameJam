using System;
using System.Collections;
using System.Collections.Generic;
using GameJam.AI;
using GameJam.Core;
using GameJam.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Runs one match in the scene. A single coroutine loop waits for the human's god card and the Pantheon powers / Classic mode, builds the
    /// Match, then issues every Core command (human clicks or bot decisions, including the Wrath)
    /// one step at a time. Core events are turned
    /// into visuals and sound by <see cref="MatchPresenter"/>; keeping commands out of event handlers
    /// respects Match's rule that handlers must not re-enter it. Buttons only set flags that the
    /// loop reads.
    /// </summary>
    public sealed class MatchController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField]
        private BoardView _board;

        [SerializeField]
        private HudView _hud;

        [SerializeField]
        private DiceView _dice;

        [SerializeField]
        private MenuView _menu;

        [Tooltip("The turn-order draw before round 1.")]
        [SerializeField]
        private TurnOrderView _turnOrder;

        [Tooltip("Read only to tell a click that skips the Destiny Die from a board click.")]
        [SerializeField]
        private DestinyView _destiny;

        [SerializeField]
        private MatchPresenter _presenter;

        [Header("Audio")]
        [Tooltip(
            "Input feedback only: clicks, refusals and placed troops. Event cues live in MatchPresenter."
        )]
        [SerializeField]
        private SfxPlayer _sfx;

        [Header("Players")]
        [Tooltip(
            "Debug: the bots also play the human's god, so a whole match plays by itself."
        )]
        [SerializeField]
        private bool _botPlaysHuman;

        [Header("Pacing (seconds)")]
        [Tooltip("Pause before each troop a bot places.")]
        [SerializeField, Min(0f)]
        private float _botPlacePause = 1f;

        [Tooltip("Pause before each bot attack decision, and while its attack is highlighted.")]
        [SerializeField, Min(0f)]
        private float _botStepPause = 1.2f;

        [Tooltip(
            "The longest a landed result of the human's own combat waits, when it needs no decision; a click continues sooner."
        )]
        [SerializeField, Min(0f)]
        private float _resultHold = 4.5f;

        [Tooltip(
            "How long the optional Offering stays under the human's die when they lose a combat they may re-roll; then the combat confirms by itself."
        )]
        [SerializeField, Min(0.1f)]
        private float _offeringWindow = 2f;

        [Tooltip(
            "How long a landed result of a bot's combat stays readable before it is confirmed."
        )]
        [SerializeField, Min(0f)]
        private float _botResultHold = 3f;

        [Tooltip(
            "Bot pauses, including the result hold of their combats and their turn banner, are multiplied by this while fast bots is on."
        )]
        [SerializeField, Range(0.05f, 1f)]
        private float _fastBotFactor = 1f / 3f;

        private Board _boardModel;
        private Match _match;

        // The human's god (1 human vs 3 bots), chosen in the menu or kept by "Play again".
        private God? _chosenGod;
        private God _human;
        private Territory _selected;

        // While the human aims a Wrath, clicks pick its target instead of an attack. Thor's Mjölnir
        // also picks its source first, kept apart from the attack selection.
        private bool _aimingWrath;
        private Territory _wrathSource;

        // Sacred Road mode, for the human only: while a Road is open and some army can march, the
        // ringed origins (own territories with 2+ troops and a path) are the only territories that react
        // until the march or END TURN; picking one rings its destinations. Both lists are filled when
        // they become known and only read after, so the hover never recomputes the Road's paths per frame.
        private bool _roadMode;
        private Territory _roadSource;
        private readonly List<Territory> _roadOrigins = new List<Territory>();
        private readonly List<Territory> _roadDestinations = new List<Territory>();

        // Set by menu, HUD and dice button events and consumed by the loop, so clicks never issue commands.
        private bool _offeringRequested;

        // True only while the Offering button is on screen, so a click queued for the frame the window
        // closes can never re-roll a combat the player already saw confirm.
        private bool _offeringOpen;
        private bool _endTurnRequested;
        private bool _wrathRequested;

        // The Wrath can be unleashed this visit to the attack loop: the attack prompt names it.
        private bool _wrathReady;

        // A board click that ended the human's combat window early. It confirms the combat and is
        // then replayed by the attack loop, so clicking the same enemy again attacks again at once.
        private Territory _carriedClick;

        // The target of the human's last attack: where the army advances after a capture.
        private Territory _lastTarget;

        // Set once the god is chosen and Match.Start has run; before that, board clicks mean nothing.
        private bool _matchStarted;

        // True only while HumanReinforce or HumanAttack waits for a board click, so pieces never
        // react to hover under the Destiny Die, the turn banner, a combat or a bot's turn.
        private bool _awaitingBoardClick;

        // The Destiny overlay was showing in the previous Update; see Update for why that matters.
        private bool _destinyWasPlaying;

        // Only read once the match started, when both exist.
        private bool IsHumanTurn => !_botPlaysHuman && _match.CurrentGod == _human;

        // The human is out: the match is over for the player even though the bots could keep playing.
        private bool AreAllHumansOut =>
            _match != null && !_botPlaysHuman && _match.IsEliminated(_human);

        private float BotPaceFactor => MatchSession.FastBots ? _fastBotFactor : 1f;

        private void Awake()
        {
            // The board shows behind the menu at once. The Match itself waits for the menu, because
            // the Pantheon powers / Classic choice is part of how it is built.
            _boardModel = Board.CreateStandard();
            _board.Build(_boardModel);
        }

        private void OnEnable()
        {
            if (_match != null)
                AttachMatch();
            _menu.GodCardClicked += HandleGodCardClicked;
            _menu.CardHovered += HandleCardHovered;
            _turnOrder.Landed += HandleDrawVerdict;
            _menu.PowersChosen += HandlePowersChosen;
            _dice.OfferingClicked += HandleOfferingClicked;
            _hud.EndTurnClicked += HandleEndTurnClicked;
            _hud.WrathClicked += HandleWrathClicked;
            _hud.WrathBlocked += HandleWrathBlocked;
            _hud.FastBotsClicked += HandleFastBotsClicked;
            _hud.MuteClicked += HandleMuteClicked;
            _hud.PlayAgainClicked += HandlePlayAgainClicked;
            _hud.MainMenuClicked += HandleMainMenuClicked;
        }

        private void OnDisable()
        {
            if (_match != null)
                DetachMatch();
            _menu.GodCardClicked -= HandleGodCardClicked;
            _menu.CardHovered -= HandleCardHovered;
            _turnOrder.Landed -= HandleDrawVerdict;
            _menu.PowersChosen -= HandlePowersChosen;
            _dice.OfferingClicked -= HandleOfferingClicked;
            _hud.EndTurnClicked -= HandleEndTurnClicked;
            _hud.WrathClicked -= HandleWrathClicked;
            _hud.WrathBlocked -= HandleWrathBlocked;
            _hud.FastBotsClicked -= HandleFastBotsClicked;
            _hud.MuteClicked -= HandleMuteClicked;
            _hud.PlayAgainClicked -= HandlePlayAgainClicked;
            _hud.MainMenuClicked -= HandleMainMenuClicked;
        }

        // Subscribed before Match.Start so the first RoundStarted and TurnStarted are not missed.
        private void AttachMatch()
        {
            _presenter.Attach(_match);
            _match.TurnStarted += HandleTurnStarted;
        }

        private void DetachMatch()
        {
            _presenter.Detach();
            _match.TurnStarted -= HandleTurnStarted;
        }

        // In Start, not Awake: the views query their elements in their own OnEnable.
        private void Start()
        {
            _hud.SetFastBots(MatchSession.FastBots);
            _hud.SetMuted(MatchSession.Muted);
            _sfx.SetMuted(MatchSession.Muted);
            _menu.SetPowers(MatchSession.PowersEnabled);
            StartCoroutine(Run());
        }

        // The loop reads board clicks only on the human's turn; during a bot's turn a click on the
        // board answers "not now" instead of doing nothing.
        private void Update()
        {
            _board.SetHovered(UsableTerritoryUnderPointer());
            // A click that skips the Destiny Die belongs to its overlay, not the board. UI Toolkit may
            // handle it before or after this Update, so the frame after the overlay last showed is
            // ignored too.
            bool destinyTookClick = _destiny.IsPlaying || _destinyWasPlaying;
            _destinyWasPlaying = _destiny.IsPlaying;
            if (destinyTookClick || !_matchStarted || IsHumanTurn || AreAllHumansOut)
                return;
            if (_match.Phase != TurnPhase.MatchOver && TryGetClickedTerritory(out _))
                _sfx.Play(SfxCue.Invalid);
        }

        // The turn state machine lives in Match; this loop only asks who acts in the current phase.
        private IEnumerator Run()
        {
            yield return ChooseSeats();
            _match.Start();
            _matchStarted = true;
            // Stopping when every person is eliminated also stops every remaining bot pause.
            while (_match.Phase != TurnPhase.MatchOver && !AreAllHumansOut)
            {
                yield return _presenter.PlayAnnouncements(_fastBotFactor);
                switch (_match.Phase)
                {
                    case TurnPhase.Reinforce:
                        yield return IsHumanTurn ? HumanReinforce() : BotReinforce();
                        break;
                    case TurnPhase.Attack:
                        yield return IsHumanTurn ? HumanAttack() : BotAttack();
                        break;
                    case TurnPhase.CombatPending:
                        yield return ResolveCombat();
                        break;
                    default:
                        // A phase nobody handles would spin this loop forever without yielding.
                        throw new InvalidOperationException(
                            $"No one acts in phase {_match.Phase}."
                        );
                }
            }
        }

        // "Play again" already knows the god and the mode; otherwise the menu waits for a god card
        // click. The Match is built here, once the mode is final.
        private IEnumerator ChooseSeats()
        {
            _chosenGod = MatchSession.ReplayGod;
            MatchSession.ReplayGod = null;
            if (_chosenGod == null)
            {
                _menu.Show();
                while (_chosenGod == null)
                    yield return null;
                _menu.Hide();
            }

            // The dice fix the turn order for the whole match, whoever the human is (Classic and
            // "Play again" draw too). The roll sound is the same single clip as every other roll.
            TurnOrderDraw draw = TurnOrderDraw.Draw(new SystemRandom());
            _match = new Match(
                _boardModel,
                new SystemRandom(),
                MatchSession.PowersEnabled,
                draw.Order
            );
            AttachMatch();
            _human = _chosenGod.Value;
            // The whole HUD and the board are already up behind the draw; the nations panel
            // re-sorts into the drawn order when it ends.
            _presenter.SetHuman(_human, _botPlaysHuman);

            _turnOrder.RollStarted = () => _sfx.PlayRoll(_dice.FallbackTumbleDuration);
            yield return _turnOrder.Play(draw, BotPaceFactor);
            _sfx.StopRoll();
            _presenter.ShowTurnOrder(draw.Order);
        }

        private IEnumerator HumanReinforce()
        {
            _hud.SetTurnStatus(MatchText.StatusYourReinforce);
            _hud.SetPrompt(MatchText.ReinforcePrompt(_match.ReinforcementsLeft));
            _awaitingBoardClick = true;
            while (true)
            {
                // Wait a frame first: the loop may re-enter in the frame of the previous click,
                // and reading that click again would place several troops at once.
                yield return null;
                if (!TryGetClickedTerritory(out Territory territory))
                    continue;

                if (territory.Owner != _match.CurrentGod)
                {
                    // Appended, not replaced, so the instruction stays visible.
                    _hud.SetPrompt(
                        MatchText.WithHint(
                            MatchText.ReinforcePrompt(_match.ReinforcementsLeft),
                            MatchText.PickOwnTerritoryHint
                        )
                    );
                    _sfx.Play(SfxCue.Invalid);
                    continue;
                }

                _hud.HideHint();
                _awaitingBoardClick = false;
                _match.PlaceTroop(territory);
                _sfx.Play(SfxCue.TroopPlaced);
                yield break;
            }
        }

        private IEnumerator HumanAttack()
        {
            // Favor and the Road only change through commands, so the Wrath button and Road mode are
            // set once per visit to this loop. Before Select, whose prompt names a ready Wrath.
            bool wrathUsable = RefreshWrathButton();
            Select(NextAttacker());
            _hud.SetTurnStatus(MatchText.StatusYourAttack);
            _endTurnRequested = false;
            _wrathRequested = false;
            _hud.ShowEndTurn(true);
            // A sacred roll's Road starts here, back in the attack loop, never over the combat's dice.
            if (StartRoadMode())
            {
                // The popup says what the rings are about, so both appear together, once the Favor
                // line of the sacred die has been read. Clicks wait for that moment, no longer.
                yield return _presenter.ShowRoadPopupIfOpened();
                ShowRoadOrigins();
            }
            else
            {
                // A Road no army can use gets no popup, now or later this turn.
                _presenter.ClearPendingRoadPopup();
                if (!(wrathUsable && _presenter.ShowHintOnce(Hint.Wrath)))
                    _presenter.ShowHintOnce(Hint.Attack);
            }
            _awaitingBoardClick = true;

            while (true)
            {
                // Same as HumanReinforce: never read the click that issued the previous command.
                yield return null;
                if (_endTurnRequested)
                {
                    // The only way to decline a Road: it is lost with the rest of the turn.
                    StopAimingWrath();
                    LeaveHumanAttack();
                    Select(null);
                    _match.EndTurn();
                    yield break;
                }

                if (_wrathRequested)
                {
                    _wrathRequested = false;
                    if (_aimingWrath)
                    {
                        CancelWrath();
                    }
                    else if (GodPowers.WrathNeedsTarget(_match.CurrentGod))
                    {
                        AimWrath();
                    }
                    else
                    {
                        // Eye of Ra and Harvest choose for themselves: the button is the whole command.
                        LeaveHumanAttack();
                        _match.UnleashWrath();
                        yield break;
                    }
                    continue;
                }

                if ((_aimingWrath || _roadSource != null) && WasCancelPressed())
                {
                    if (_aimingWrath)
                        CancelWrath();
                    else
                        DropRoadOrigin();
                    continue;
                }

                if (!TryTakeAttackClick(out Territory clicked))
                    continue;

                if (_aimingWrath)
                {
                    if (!TryAimWrathAt(clicked))
                        continue;
                    Territory source = _wrathSource;
                    StopAimingWrath();
                    LeaveHumanAttack();
                    _match.UnleashWrath(source, clicked);
                    yield break;
                }

                if (_roadMode)
                {
                    if (!TryRoadClick(clicked))
                        continue;
                    Territory origin = _roadSource;
                    LeaveHumanAttack();
                    _match.UseRoad(origin, clicked);
                    yield break;
                }

                if (_selected != null && _match.CanAttack(_selected, clicked))
                {
                    LeaveHumanAttack();
                    _lastTarget = clicked;
                    // The march line starts with the attack, so the dice that follow roll for it.
                    _presenter.ShowAttackPath(_selected, clicked);
                    _match.Attack(_selected, clicked);
                    yield break;
                }
                SelectOrExplain(clicked);
            }
        }

        // The attack loop's buttons and hint go away before a command. The selection stays, so an
        // attack keeps its rings during the combat and the next visit can reselect it. Road mode ends
        // with any command (a march spends the Road, a Wrath or END TURN leaves the loop). The prompt is
        // dropped with them: "Choose a target…" must not hang over the dice it just started.
        private void LeaveHumanAttack()
        {
            _awaitingBoardClick = false;
            _lastTarget = null;
            _hud.ShowEndTurn(false);
            _hud.ShowWrath(false);
            _hud.HideHint();
            _hud.SetPrompt(string.Empty);
            if (!_roadMode)
                return;
            StopRoadMode();
            _presenter.HideRoadPopup();
        }

        // Risk-style follow-up after a combat: the attacker stays selected while it can still attack,
        // so the same enemy is one click away. After a capture the source keeps a single troop, so
        // the selection follows the army into the captured territory when that one can attack.
        private Territory NextAttacker()
        {
            Territory advanced = _lastTarget;
            _lastTarget = null;
            if (_selected == null)
                return null;
            if (HasTarget(_selected))
                return _selected;
            bool canAdvance =
                advanced != null && advanced.Owner == _match.CurrentGod && HasTarget(advanced);
            return canAdvance ? advanced : null;
        }

        // The click that ended the combat window comes first, then a fresh click on the board.
        private bool TryTakeAttackClick(out Territory clicked)
        {
            clicked = _carriedClick;
            _carriedClick = null;
            return clicked != null || TryGetClickedTerritory(out clicked);
        }

        /// <summary>Shows the Wrath button (powers on) and returns whether it can be pressed now.</summary>
        private bool RefreshWrathButton()
        {
            _hud.ShowWrath(_match.PowersEnabled);
            if (!_match.PowersEnabled)
                return false;

            God god = _match.CurrentGod;
            bool usable = _match.IsWrathReady && HasWrathTarget();
            _wrathReady = usable;
            // One Wrath per turn: once unleashed the button says so until the next turn; otherwise it is
            // ready, or locked (short of Favor, or full with nothing to strike).
            WrathButtonState state =
                usable ? WrathButtonState.Ready
                : _match.WrathUsedThisTurn ? WrathButtonState.Used
                : WrathButtonState.Locked;
            _hud.SetWrath(god, _match.FavorOf(god), state);
            return usable;
        }

        // Ra and Morrígan have no target; Zeus may strike any territory; Thor needs a source next to it.
        private bool HasWrathTarget()
        {
            if (!GodPowers.WrathNeedsTarget(_match.CurrentGod))
                return _match.CanUnleashWrath();
            foreach (Territory territory in _match.Board.Territories)
            {
                bool aimable = GodPowers.WrathNeedsSource(_match.CurrentGod)
                    ? HasWrathTargetFrom(territory)
                    : _match.CanUnleashWrath(target: territory);
                if (aimable)
                    return true;
            }
            return false;
        }

        // An index loop for the same reason as HasTarget: the hover calls this every frame.
        private bool HasWrathTargetFrom(Territory source)
        {
            IReadOnlyList<Territory> neighbors = source.Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (_match.CanUnleashWrath(source, neighbors[i]))
                    return true;
            }
            return false;
        }

        private void AimWrath()
        {
            God god = _match.CurrentGod;
            _aimingWrath = true;
            // The Wrath suspends Road mode (its highlights are redrawn); cancelling restores it.
            _roadSource = null;
            _roadDestinations.Clear();
            // Thor aims from the selected attacker when it can strike, saving a click.
            bool selectedCanStrike =
                GodPowers.WrathNeedsSource(god)
                && _selected != null
                && HasWrathTargetFrom(_selected);
            _wrathSource = selectedCanStrike ? _selected : null;
            _hud.SetWrath(god, _match.FavorOf(god), WrathButtonState.Aiming);
            _hud.HideHint();
            ShowWrathTargets();
            _sfx.Play(SfxCue.Click);
        }

        // Backing out of the Wrath goes back to what it interrupted: the ringed Road origins when a
        // Road is pending, else the plain attack mode.
        private void CancelWrath()
        {
            StopAimingWrath();
            RefreshWrathButton();
            if (_roadMode)
                ShowRoadOrigins();
            else
                Select(null);
            _sfx.Play(SfxCue.Click);
        }

        private void StopAimingWrath()
        {
            _aimingWrath = false;
            _wrathSource = null;
        }

        // Rings every legal target with the troops the Wrath would take ("-3"), so the choice is read
        // before the click. Thor first rings nothing and waits for his source, like an attack.
        private void ShowWrathTargets()
        {
            _board.ClearHighlights();
            God god = _match.CurrentGod;
            SetWrathPrompt(MatchText.WrathTargetPrompt(god, _wrathSource));
            bool needsSource = GodPowers.WrathNeedsSource(god);
            if (needsSource && _wrathSource == null)
                return;
            if (needsSource)
                _board.SetHighlight(_wrathSource, TerritoryHighlight.Selected);

            foreach (Territory target in _match.Board.Territories)
            {
                if (!_match.CanUnleashWrath(_wrathSource, target))
                    continue;
                _board.SetHighlight(target, TerritoryHighlight.Target);
                int loss = needsSource
                    ? GodPowers.MjolnirLoss(target.Troops)
                    : GodPowers.ThunderboltLoss(target.Troops);
                _board.SetPreview(target, -loss);
            }
        }

        // True when the click is the Wrath's target. A click on one of Thor's own territories picks
        // (or drops) his source instead; anything else says why it cannot be struck.
        private bool TryAimWrathAt(Territory clicked)
        {
            if (_match.CanUnleashWrath(_wrathSource, clicked))
                return true;

            God god = _match.CurrentGod;
            if (GodPowers.WrathNeedsSource(god) && clicked.Owner == god)
            {
                bool canStrikeFromHere = clicked != _wrathSource && HasWrathTargetFrom(clicked);
                if (canStrikeFromHere || clicked == _wrathSource)
                {
                    _wrathSource = canStrikeFromHere ? clicked : null;
                    ShowWrathTargets();
                    _sfx.Play(SfxCue.Click);
                    return false;
                }
                SetWrathPrompt(
                    MatchText.WithHint(
                        MatchText.WrathTargetPrompt(god, _wrathSource),
                        MatchText.WrathNoSourceHint
                    )
                );
                _sfx.Play(SfxCue.Invalid);
                return false;
            }

            // Thor without a source must start from his own side, whatever enemy was clicked.
            string why =
                GodPowers.WrathNeedsSource(god) && _wrathSource == null
                    ? MatchText.WrathPickSourceFirstHint
                    : MatchText.WrathNoTargetHint(god);
            SetWrathPrompt(MatchText.WithHint(MatchText.WrathTargetPrompt(god, _wrathSource), why));
            _sfx.Play(SfxCue.Invalid);
            return false;
        }

        // The prompt bar names the Wrath being aimed ("THUNDERBOLT") where it usually says whose turn it is.
        private void SetWrathPrompt(string text) =>
            _hud.SetLabeledPrompt(MatchText.WrathButton(_match.CurrentGod), text);

        /// <summary>
        /// Enters Road mode when the human holds an open Road and has an army that can march (2+
        /// troops and a destination); otherwise nothing is blocked. Called once per visit to the
        /// attack loop: the Road only opens through a combat, so this never runs per frame.
        /// </summary>
        private bool StartRoadMode()
        {
            StopRoadMode();
            if (!_match.PowersEnabled || !_match.IsRoadAvailable)
                return false;
            CollectRoadOrigins();
            if (_roadOrigins.Count == 0)
                return false;

            _roadMode = true;
            // No attack selection while the Road is pending; the ringed origins are the only choices.
            _selected = null;
            return true;
        }

        private void StopRoadMode()
        {
            _roadMode = false;
            _roadSource = null;
            _roadOrigins.Clear();
            _roadDestinations.Clear();
        }

        // Own territories that can march: 2+ troops and at least one destination.
        private void CollectRoadOrigins()
        {
            _roadOrigins.Clear();
            God god = _match.CurrentGod;
            foreach (Territory territory in _match.Board.Territories)
            {
                if (
                    territory.Owner == god
                    && GodPowers.RoadTroops(territory.Troops) > 0
                    && GodPowers.RoadDestinations(_match.Board, god, territory).Count > 0
                )
                    _roadOrigins.Add(territory);
            }
        }

        // Every army that can march wears the Road ring and its ROAD tag; everything else stays plain.
        private void ShowRoadOrigins()
        {
            _roadSource = null;
            _roadDestinations.Clear();
            _board.ClearHighlights();
            for (int i = 0; i < _roadOrigins.Count; i++)
                _board.SetHighlight(_roadOrigins[i], TerritoryHighlight.RoadOrigin);
            _hud.SetRoadPrompt(MatchText.RoadOriginsPrompt);
        }

        // The destinations are looked up once, here, and cached for the highlights, the hover and the
        // click. The other origins go plain, so only what the click can do glows: the picked army
        // (lifted) and its ringed destinations with the troops that would march ("+9").
        private void PickRoadOrigin(Territory source)
        {
            _roadSource = source;
            // The person acted on the popup's advice: it has done its job.
            _presenter.HideRoadPopup();
            _roadDestinations.Clear();
            _roadDestinations.AddRange(
                GodPowers.RoadDestinations(_match.Board, _match.CurrentGod, source)
            );

            _board.ClearHighlights();
            _board.SetHighlight(source, TerritoryHighlight.Selected);
            int moved = GodPowers.RoadTroops(source.Troops);
            for (int i = 0; i < _roadDestinations.Count; i++)
            {
                _board.SetHighlight(_roadDestinations[i], TerritoryHighlight.RoadDestination);
                _board.SetPreview(_roadDestinations[i], moved);
            }
            _hud.SetRoadPrompt(MatchText.RoadDestinationsPrompt);
        }

        // Esc, right-click or a second click on the origin: back to the ringed origins (never to attacking).
        private void DropRoadOrigin()
        {
            ShowRoadOrigins();
            _sfx.Play(SfxCue.Click);
        }

        // True when the click is a destination: the march is chosen. Otherwise it picks or drops an
        // origin, or says why nothing happened (attacks wait until the Road is resolved); the hint
        // replaces the prompt until the next valid click.
        private bool TryRoadClick(Territory clicked)
        {
            if (_roadSource != null && _match.CanUseRoad(_roadSource, clicked))
                return true;

            if (_roadSource == null && _roadOrigins.Contains(clicked))
            {
                PickRoadOrigin(clicked);
                _sfx.Play(SfxCue.Click);
                return false;
            }
            if (clicked == _roadSource)
            {
                DropRoadOrigin();
                return false;
            }

            string why =
                _roadSource != null ? MatchText.RoadNoDestinationHint
                : clicked.Owner == _match.CurrentGod ? MatchText.RoadNoOriginHint
                : MatchText.RoadAttackWaitsHint;
            _hud.SetRoadPrompt(why);
            _sfx.Play(SfxCue.Invalid);
            return false;
        }

        // Right-click or Esc, the usual "never mind" of a desktop game.
        private bool WasCancelPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                return true;
            // A right-click over the HUD is not a "never mind" for the aiming.
            return mouse != null
                && mouse.rightButton.wasPressedThisFrame
                && !_hud.IsPointerOverUi(mouse.position.ReadValue());
        }

        private IEnumerator ResolveCombat()
        {
            while (_dice.IsAnimating)
                yield return null;

            // A person defending against a bot still gets the combat hint.
            bool canOffer = IsHumanTurn && _match.CanMakeOffering;
            _presenter.ShowCombatHint(canOffer);
            if (!IsHumanTurn)
            {
                yield return BotPause(_botResultHold);
                if (BotPlayer.ShouldMakeOffering(_match))
                    _match.MakeOffering();
                else
                    _match.ConfirmCombat();
                yield break;
            }

            yield return HumanResultWindow(canOffer);
            // After an Offering the phase stays CombatPending, so the loop comes back here with the new roll.
            if (_offeringRequested)
            {
                _offeringRequested = false;
                _hud.HideHint();
                _hud.SetPrompt(MatchText.OfferingMadePrompt);
                _match.MakeOffering();
            }
            else
            {
                _match.ConfirmCombat();
            }
        }

        // The human's landed result never asks for a decision: it confirms by itself after the hold
        // (the Offering window when a re-roll is possible), or sooner on a click on the board. That
        // click is kept for the attack loop, so attacking again is a single click, like in Risk.
        // The Offering button, when shown, is the only way to re-roll; it never blocks the game.
        private IEnumerator HumanResultWindow(bool canOffer)
        {
            float window = canOffer ? _offeringWindow : _resultHold;
            _offeringRequested = false;
            _offeringOpen = canOffer;
            _dice.ShowOffering(canOffer);
            _dice.ShowContinueHint(!canOffer);

            for (float elapsed = 0f; elapsed < window; elapsed += Time.deltaTime)
            {
                // Yield first, so a click is only read in a frame after the result showed.
                yield return null;
                if (canOffer)
                    _dice.SetOfferingTimeLeft(1f - elapsed / window);
                if (_offeringRequested)
                    break;
                if (WasBoardPressed(out Vector2 position))
                {
                    _board.TryPick(position, out _carriedClick);
                    break;
                }
            }
            _offeringOpen = false;
            _dice.ShowOffering(false);
            _dice.ShowContinueHint(false);
        }

        private IEnumerator BotReinforce()
        {
            _hud.SetTurnStatus(MatchText.StatusReinforcing);
            yield return BotPause(_botPlacePause);
            _match.PlaceTroop(BotPlayer.ChooseReinforcement(_match));
            _sfx.Play(SfxCue.TroopPlaced);
        }

        private IEnumerator BotAttack()
        {
            _board.ClearHighlights();
            _hud.SetTurnStatus(MatchText.StatusThinking);
            yield return BotPause(_botStepPause);
            // Asked before every attack: Favor can fill up in the middle of the turn.
            if (BotPlayer.TryChooseWrath(_match, out Territory source, out Territory target))
            {
                yield return BotWrath(source, target);
                yield break;
            }
            // The march comes before the attack: it brings the army to where the fight is.
            if (BotPlayer.TryChooseRoad(_match, out Territory roadFrom, out Territory roadTo))
            {
                yield return BotRoad(roadFrom, roadTo);
                yield break;
            }
            if (!BotPlayer.TryChooseAttack(_match, out Territory from, out Territory to))
            {
                _match.EndTurn();
                yield break;
            }

            // Show the intended attack first so the player can follow what the bot does.
            _hud.SetTurnStatus(MatchText.StatusAttacking);
            _board.SetHighlight(from, TerritoryHighlight.Selected);
            _board.SetHighlight(to, TerritoryHighlight.Target);
            yield return BotPause(_botStepPause);
            _presenter.ShowAttackPath(from, to);
            _match.Attack(from, to);
        }

        // Paced like an attack: the aim shows first, then the blow, then its result stays readable.
        private IEnumerator BotWrath(Territory source, Territory target)
        {
            God god = _match.CurrentGod;
            if (source != null)
                _board.SetHighlight(source, TerritoryHighlight.Selected);
            if (target != null)
                _board.SetHighlight(target, TerritoryHighlight.Target);
            _hud.SetTurnStatus(MatchText.StatusWrath(god));
            yield return BotPause(_botStepPause);

            _match.UnleashWrath(source, target);
            yield return BotPause(_botResultHold);
            // A Wrath that ended the match, or knocked the last person out, leaves the end screen alone.
            if (_match.Phase != TurnPhase.MatchOver && !AreAllHumansOut)
                _hud.SetTurnStatus(MatchText.StatusThinking);
        }

        // Paced like an attack: the origin and destination show first, then the march; the presenter
        // plays the path and the turn loop waits for it before anyone acts.
        private IEnumerator BotRoad(Territory from, Territory to)
        {
            _board.SetHighlight(from, TerritoryHighlight.Selected);
            _board.SetHighlight(to, TerritoryHighlight.Target);
            _hud.SetTurnStatus(MatchText.StatusMarching);
            yield return BotPause(_botStepPause);

            _match.UseRoad(from, to);
            yield return BotPause(_botStepPause);
            if (_match.Phase != TurnPhase.MatchOver && !AreAllHumansOut)
                _hud.SetTurnStatus(MatchText.StatusThinking);
        }

        // The factor is read every frame, so toggling fast bots mid-pause takes effect at once.
        private IEnumerator BotPause(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds * BotPaceFactor; elapsed += Time.deltaTime)
                yield return null;
        }

        private Territory UsableTerritoryUnderPointer()
        {
            if (!_awaitingBoardClick)
                return null;
            Pointer pointer = Pointer.current;
            if (pointer == null)
                return null;

            Vector2 position = pointer.position.ReadValue();
            if (
                _hud.IsPointerOverUi(position) || !_board.TryPick(position, out Territory territory)
            )
                return null;
            return IsUsableNow(territory) ? territory : null;
        }

        // The same rules the click handlers apply, so a hover never promises a click that is refused.
        private bool IsUsableNow(Territory territory)
        {
            if (_match.Phase == TurnPhase.Reinforce)
                return territory.Owner == _match.CurrentGod;
            if (_aimingWrath)
                return IsWrathClickUsable(territory);
            if (_roadMode)
                return IsRoadClickUsable(territory);
            if (_selected != null)
                return _match.CanAttack(_selected, territory);
            return HasTarget(territory);
        }

        // Road mode: an origin, or once one is picked, it again (to drop it) and its destinations.
        // Cached lists only.
        private bool IsRoadClickUsable(Territory territory) =>
            _roadSource == null
                ? _roadOrigins.Contains(territory)
                : territory == _roadSource || _roadDestinations.Contains(territory);

        // While aiming: a target, or (Thor) one of his territories that can strike from there.
        private bool IsWrathClickUsable(Territory territory)
        {
            if (_match.CanUnleashWrath(_wrathSource, territory))
                return true;
            God god = _match.CurrentGod;
            return GodPowers.WrathNeedsSource(god)
                && territory.Owner == god
                && HasWrathTargetFrom(territory);
        }

        // An index loop, not foreach: this runs every frame from the hover in Update, and foreach
        // over an IReadOnlyList boxes its enumerator.
        private bool HasTarget(Territory from)
        {
            IReadOnlyList<Territory> neighbors = from.Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (_match.CanAttack(from, neighbors[i]))
                    return true;
            }
            return false;
        }

        // Clicking the selection again deselects. Anything else that cannot attack deselects and
        // says why; the next valid click sets the normal prompt again.
        private void SelectOrExplain(Territory clicked)
        {
            if (clicked != _selected && HasTarget(clicked))
            {
                Select(clicked);
                _sfx.Play(SfxCue.Click);
                return;
            }

            bool clickedSelection = clicked == _selected;
            Select(null);
            if (clickedSelection)
                return;
            _hud.SetPrompt(WhyCannotAttackFrom(clicked));
            _sfx.Play(SfxCue.Invalid);
        }

        private string WhyCannotAttackFrom(Territory territory)
        {
            if (territory.Owner != _match.CurrentGod)
                return MatchText.PickOwnTerritoryHint;
            if (territory.Troops < Match.MinTroopsToAttack)
                return MatchText.NeedsTroopsHint;
            return MatchText.NoTargetsHint;
        }

        private void Select(Territory territory)
        {
            _selected = territory;
            _board.ClearHighlights();
            if (territory == null)
            {
                _hud.SetPrompt(
                    _wrathReady
                        ? MatchText.AttackPromptWrath(_match.CurrentGod)
                        : MatchText.AttackPrompt
                );
                return;
            }

            _board.SetHighlight(territory, TerritoryHighlight.Selected);
            foreach (Territory neighbor in territory.Neighbors)
            {
                if (_match.CanAttack(territory, neighbor))
                    _board.SetHighlight(neighbor, TerritoryHighlight.Target);
            }
            _hud.SetPrompt(MatchText.TargetPrompt(territory));
        }

        private bool TryGetClickedTerritory(out Territory territory)
        {
            territory = null;
            return WasBoardPressed(out Vector2 position) && _board.TryPick(position, out territory);
        }

        // A press this frame outside the HUD's buttons and panels, so toggling sound or fast bots
        // never counts as a board click.
        private bool WasBoardPressed(out Vector2 position)
        {
            position = default;
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return false;

            position = pointer.position.ReadValue();
            return !_hud.IsPointerOverUi(position);
        }

        // A new turn starts clean: nothing selected, no Wrath being aimed and no button press left
        // over. The presenter clears the highlights.
        private void HandleTurnStarted(God god, int reinforcements)
        {
            _selected = null;
            StopAimingWrath();
            StopRoadMode();
            _offeringRequested = false;
            _carriedClick = null;
            _lastTarget = null;
            _endTurnRequested = false;
            _wrathRequested = false;
        }

        // The card starts the match as that god. On WebGL the browser only allows audio after a user
        // gesture; this click provides it.
        private void HandleCardHovered() => _sfx.Play(SfxCue.CardHover);

        // Every duel verdict of the draw (tie re-rolls included) gets the bot-result sound.
        private void HandleDrawVerdict() => _sfx.Play(SfxCue.BotResult);

        private void HandleGodCardClicked(God god)
        {
            _sfx.Play(SfxCue.Click);
            _chosenGod = god;
        }

        private void HandleOfferingClicked()
        {
            if (!_offeringOpen)
                return;
            _offeringRequested = true;
            _sfx.Play(SfxCue.Click);
        }

        private void HandleWrathClicked() => _wrathRequested = true;

        // A click on a locked or used Wrath: say what it needs, and shake it a little. Only while the
        // person is in the attack loop (the button is hidden otherwise).
        private void HandleWrathBlocked()
        {
            if (!_awaitingBoardClick || _aimingWrath)
                return;
            God god = _match.CurrentGod;
            string hint = MatchText.WrathLockedHint(
                god,
                _match.FavorOf(god),
                _match.WrathUsedThisTurn
            );
            if (_roadMode)
                _hud.SetRoadPrompt(hint);
            else
                _hud.SetPrompt(hint);
            _hud.ShakeWrath();
            _sfx.Play(SfxCue.Invalid);
            if (_wrathHintRoutine != null)
                StopCoroutine(_wrathHintRoutine);
            _wrathHintRoutine = StartCoroutine(RestorePromptAfterWrathHint(hint));
        }

        private Coroutine _wrathHintRoutine;

        // The hint is a short answer, not the new prompt: after a moment the real instruction returns,
        // unless something else (a click, a refusal) already replaced the hint.
        private IEnumerator RestorePromptAfterWrathHint(string hint)
        {
            for (float t = 0f; t < 2f; t += Time.deltaTime)
                yield return null;
            _wrathHintRoutine = null;
            if (!_awaitingBoardClick || _aimingWrath || _hud.PromptText != hint)
                yield break;
            if (_roadMode)
                _hud.SetRoadPrompt(
                    _roadSource != null
                        ? MatchText.RoadDestinationsPrompt
                        : MatchText.RoadOriginsPrompt
                );
            else if (_selected != null)
                _hud.SetPrompt(MatchText.TargetPrompt(_selected));
            else
                _hud.SetPrompt(
                    _wrathReady
                        ? MatchText.AttackPromptWrath(_match.CurrentGod)
                        : MatchText.AttackPrompt
                );
        }

        // Only reachable on the menu, before the Match exists, so the mode is final once a card is clicked.
        private void HandlePowersChosen(bool enabled)
        {
            MatchSession.PowersEnabled = enabled;
            _menu.SetPowers(enabled);
            _sfx.Play(SfxCue.Click);
        }

        private void HandleEndTurnClicked()
        {
            _endTurnRequested = true;
            _sfx.Play(SfxCue.Click);
        }

        private void HandleFastBotsClicked()
        {
            MatchSession.FastBots = !MatchSession.FastBots;
            _hud.SetFastBots(MatchSession.FastBots);
            _sfx.Play(SfxCue.Click);
        }

        // The click plays after the change, so unmuting is confirmed by a sound and muting is silent.
        private void HandleMuteClicked()
        {
            MatchSession.Muted = !MatchSession.Muted;
            _hud.SetMuted(MatchSession.Muted);
            _sfx.SetMuted(MatchSession.Muted);
            _sfx.Play(SfxCue.Click);
        }

        // Restarting is a scene reload: Match, board and views are rebuilt from scratch, so no
        // state can leak between matches. Only the human's god crosses over, through MatchSession.
        private void HandlePlayAgainClicked()
        {
            MatchSession.ReplayGod = _human;
            ReloadScene();
        }

        private void HandleMainMenuClicked()
        {
            MatchSession.ReplayGod = null;
            ReloadScene();
        }

        private static void ReloadScene() =>
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
