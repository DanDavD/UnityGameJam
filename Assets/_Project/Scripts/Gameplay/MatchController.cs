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
    /// Runs one match in the scene. A single coroutine loop waits for the god choice, then issues
    /// every Core command (human clicks or bot decisions) one step at a time; Core event handlers
    /// only update visuals. Keeping commands out of handlers respects Match's rule that handlers
    /// must not re-enter it. Buttons only set flags that the loop reads.
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

        [SerializeField]
        private NationsView _nations;

        [SerializeField]
        private TurnBannerView _turnBanner;

        [SerializeField]
        private DestinyView _destiny;

        [Header("Players")]
        [Tooltip("Debug: a bot also plays the human's nation, so a whole match plays by itself.")]
        [SerializeField]
        private bool _botPlaysHuman;

        [Header("Pacing (seconds)")]
        [Tooltip("Pause before each troop a bot places.")]
        [SerializeField, Min(0f)]
        private float _botPlacePause = 0.25f;

        [Tooltip("Pause before each bot attack decision, and while its attack is highlighted.")]
        [SerializeField, Min(0f)]
        private float _botStepPause = 0.5f;

        [Tooltip("How long a landed combat result stays readable before it is confirmed.")]
        [SerializeField, Min(0f)]
        private float _resultHold = 1.5f;

        [Tooltip(
            "Bot pauses, including the result hold of their combats, are multiplied by this while fast bots is on."
        )]
        [SerializeField, Range(0.05f, 1f)]
        private float _fastBotFactor = 1f / 3f;

        private Match _match;
        private God _humanGod;
        private Territory _selected;

        // Set by menu and HUD button events and consumed by the loop, so clicks never issue commands.
        private God? _chosenGod;
        private bool _offeringRequested;
        private bool _acceptRequested;
        private bool _endTurnRequested;

        // Set on TurnStarted and played by the loop, after the Destiny Die when a round also started.
        private God? _pendingTurnBanner;

        // The HUD round line waits for the Destiny Die to land, so it never spoils the roll.
        private string _pendingRoundLine;

        // Territories the bad omen took a troop from while the Destiny Die was still rolling. Their
        // pieces are redrawn after it lands, for the same reason. At most one per nation.
        private readonly List<Territory> _omenLosses = new List<Territory>(
            Match.BaseTurnOrder.Count
        );

        // The human is out: the match is over for them even though the bots could keep playing.
        private bool _humanEliminated;

        // End-screen stat and one-time onboarding hints; both live only for this match (scene load).
        private int _combatsWonByHuman;
        private readonly HashSet<Hint> _shownHints = new HashSet<Hint>();

        private bool IsHumanTurn => !_botPlaysHuman && _match.CurrentGod == _humanGod;

        private float BotPaceFactor => MatchSession.FastBots ? _fastBotFactor : 1f;

        private void Awake()
        {
            // Built at once so the board shows behind the menu; the match starts after the choice.
            _match = new Match(Board.CreateStandard(), new SystemRandom());
            _board.Build(_match.Board);
        }

        // Subscribed before Start() so the first RoundStarted and TurnStarted are not missed.
        private void OnEnable()
        {
            _match.RoundStarted += HandleRoundStarted;
            _match.TurnStarted += HandleTurnStarted;
            _match.TerritoryChanged += HandleTerritoryChanged;
            _match.TerritoryCaptured += HandleTerritoryCaptured;
            _match.CombatRolled += HandleCombatRolled;
            _match.CombatResolved += HandleCombatResolved;
            _match.NationEliminated += HandleNationEliminated;
            _match.MatchWon += HandleMatchWon;
            _menu.GodChosen += HandleGodChosen;
            _hud.OfferingClicked += HandleOfferingClicked;
            _hud.AcceptClicked += HandleAcceptClicked;
            _hud.EndTurnClicked += HandleEndTurnClicked;
            _hud.FastBotsClicked += HandleFastBotsClicked;
            _hud.PlayAgainClicked += HandlePlayAgainClicked;
            _hud.MainMenuClicked += HandleMainMenuClicked;
        }

        private void OnDisable()
        {
            _match.RoundStarted -= HandleRoundStarted;
            _match.TurnStarted -= HandleTurnStarted;
            _match.TerritoryChanged -= HandleTerritoryChanged;
            _match.TerritoryCaptured -= HandleTerritoryCaptured;
            _match.CombatRolled -= HandleCombatRolled;
            _match.CombatResolved -= HandleCombatResolved;
            _match.NationEliminated -= HandleNationEliminated;
            _match.MatchWon -= HandleMatchWon;
            _menu.GodChosen -= HandleGodChosen;
            _hud.OfferingClicked -= HandleOfferingClicked;
            _hud.AcceptClicked -= HandleAcceptClicked;
            _hud.EndTurnClicked -= HandleEndTurnClicked;
            _hud.FastBotsClicked -= HandleFastBotsClicked;
            _hud.PlayAgainClicked -= HandlePlayAgainClicked;
            _hud.MainMenuClicked -= HandleMainMenuClicked;
        }

        // In Start, not Awake: the views query their elements in their own OnEnable.
        private void Start()
        {
            _hud.SetFastBots(MatchSession.FastBots);
            StartCoroutine(Run());
        }

        // The turn state machine lives in Match; this loop only asks who acts in the current phase.
        private IEnumerator Run()
        {
            yield return ChooseGod();
            _match.Start();
            // Stopping when the human is eliminated also stops every remaining bot pause.
            while (_match.Phase != TurnPhase.MatchOver && !_humanEliminated)
            {
                yield return PlayAnnouncements();
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

        // "Play again" already knows the god; otherwise the menu waits for a card click.
        private IEnumerator ChooseGod()
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

            _humanGod = _chosenGod.Value;
            _hud.SetIdentity(_humanGod, MatchText.IdentityLine(_humanGod));
            _nations.SetHuman(_humanGod);
            _nations.Refresh(_match);
        }

        // The Destiny Die (when a round just started), then the turn banner, before anyone acts.
        private IEnumerator PlayAnnouncements()
        {
            while (_destiny.IsPlaying)
                yield return null;
            bool roundStarted = _pendingRoundLine != null;
            ApplyPendingRoundLine();
            ShowOmenLosses();
            // After the die, not under its overlay, so the hint is readable when it explains the roll.
            if (roundStarted)
                ShowHintOnce(Hint.Destiny);

            if (_pendingTurnBanner == null)
                yield break;

            God god = _pendingTurnBanner.Value;
            _pendingTurnBanner = null;
            // The turn highlight moves with the banner, not before the Destiny Die has landed.
            _nations.Refresh(_match);
            bool human = IsHumanTurn;
            _turnBanner.Show(god, MatchText.TurnBanner(god, human), human ? 1f : BotPaceFactor);
            while (_turnBanner.IsPlaying)
                yield return null;
        }

        private void ShowOmenLosses()
        {
            if (_omenLosses.Count == 0)
                return;
            foreach (Territory territory in _omenLosses)
            {
                _board.Refresh(territory);
                _board.ShowTroopLoss(territory);
            }
            _omenLosses.Clear();
            _nations.Refresh(_match);
        }

        private void ApplyPendingRoundLine()
        {
            if (_pendingRoundLine == null)
                return;
            _hud.SetRound(_pendingRoundLine);
            _pendingRoundLine = null;
        }

        private IEnumerator HumanReinforce()
        {
            _hud.SetPrompt(
                MatchText.ReinforcePrompt(_match.ReinforcementsLeft, _match.TurnReinforcements)
            );
            while (true)
            {
                // Wait a frame first: the loop may re-enter in the frame of the previous click,
                // and reading that click again would place several troops at once.
                yield return null;
                // Waits for a free hint slot, so it never replaces the Destiny hint the moment it appears.
                if (!_hud.IsHintVisible)
                    ShowHintOnce(Hint.Reinforce);
                if (!TryGetClickedTerritory(out Territory territory))
                    continue;

                if (territory.Owner != _match.CurrentGod)
                {
                    // Appended, not replaced, so the troop count and its breakdown stay visible.
                    _hud.SetPrompt(
                        MatchText.WithHint(
                            MatchText.ReinforcePrompt(
                                _match.ReinforcementsLeft,
                                _match.TurnReinforcements
                            ),
                            MatchText.PickOwnTerritoryHint
                        )
                    );
                    continue;
                }

                _hud.HideHint();
                _match.PlaceTroop(territory);
                yield break;
            }
        }

        private IEnumerator HumanAttack()
        {
            // Keep the previous selection after a combat so repeated attacks need fewer clicks.
            Select(_selected != null && HasTarget(_selected) ? _selected : null);
            _endTurnRequested = false;
            _hud.ShowEndTurn(true);
            ShowHintOnce(Hint.Attack);

            while (true)
            {
                // Same as HumanReinforce: never read the click that issued the previous command.
                yield return null;
                if (_endTurnRequested)
                {
                    Select(null);
                    _hud.ShowEndTurn(false);
                    _hud.HideHint();
                    _match.EndTurn();
                    yield break;
                }

                if (TryGetClickedTerritory(out Territory clicked))
                {
                    if (_selected != null && _match.CanAttack(_selected, clicked))
                    {
                        _hud.ShowEndTurn(false);
                        _hud.HideHint();
                        _match.Attack(_selected, clicked);
                        yield break;
                    }
                    SelectOrExplain(clicked);
                }
            }
        }

        private IEnumerator ResolveCombat()
        {
            while (_dice.IsAnimating)
                yield return null;

            bool humanCanOffer = IsHumanTurn && _match.CanMakeOffering;
            ShowCombatHint(humanCanOffer);
            if (humanCanOffer)
            {
                yield return HumanCombatChoice();
                yield break;
            }

            yield return IsHumanTurn ? Pause(_resultHold) : BotPause(_resultHold);
            if (!IsHumanTurn && BotPlayer.ShouldMakeOffering(_match))
                _match.MakeOffering();
            else
                _match.ConfirmCombat();
        }

        private IEnumerator HumanCombatChoice()
        {
            _hud.SetPrompt(MatchText.OfferingPrompt);
            _offeringRequested = false;
            _acceptRequested = false;
            _hud.ShowCombatChoice(true);
            while (!_offeringRequested && !_acceptRequested)
                yield return null;

            _hud.ShowCombatChoice(false);
            _hud.HideHint();
            // After an Offering the phase stays CombatPending, so the loop comes back here with the new roll.
            if (_offeringRequested)
            {
                _hud.SetPrompt(MatchText.OfferingMadePrompt);
                _match.MakeOffering();
            }
            else
            {
                _match.ConfirmCombat();
            }
        }

        private IEnumerator BotReinforce()
        {
            yield return BotPause(_botPlacePause);
            _match.PlaceTroop(BotPlayer.ChooseReinforcement(_match));
        }

        private IEnumerator BotAttack()
        {
            _board.ClearHighlights();
            yield return BotPause(_botStepPause);
            if (!BotPlayer.TryChooseAttack(_match, out Territory from, out Territory to))
            {
                _match.EndTurn();
                yield break;
            }

            // Show the intended attack first so the player can follow what the bot does.
            _board.SetHighlight(from, TerritoryHighlight.Selected);
            _board.SetHighlight(to, TerritoryHighlight.Target);
            yield return BotPause(_botStepPause);
            _match.Attack(from, to);
        }

        // The factor is read every frame, so toggling fast bots mid-pause takes effect at once.
        private IEnumerator BotPause(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds * BotPaceFactor; elapsed += Time.deltaTime)
                yield return null;
        }

        private static IEnumerator Pause(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
                yield return null;
        }

        // The first combat the human takes part in teaches its numbers. The first time the human
        // can make an Offering, that hint goes first and the numbers wait for the next combat.
        private void ShowCombatHint(bool humanCanOffer)
        {
            Combat combat = _match.PendingCombat;
            if (_botPlaysHuman || (combat.Attacker != _humanGod && combat.Defender != _humanGod))
                return;
            if (humanCanOffer && ShowHintOnce(Hint.Offering))
                return;
            ShowHintOnce(Hint.Combat);
        }

        /// <summary>Shows the hint the first time its situation happens in this match; false afterwards.</summary>
        private bool ShowHintOnce(Hint hint)
        {
            if (_botPlaysHuman || !_shownHints.Add(hint))
                return false;
            _hud.ShowHint(MatchText.HintText(hint, _humanGod));
            return true;
        }

        private bool HasTarget(Territory from)
        {
            foreach (Territory neighbor in from.Neighbors)
            {
                if (_match.CanAttack(from, neighbor))
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
                return;
            }

            bool clickedSelection = clicked == _selected;
            Select(null);
            if (!clickedSelection)
                _hud.SetPrompt(WhyCannotAttackFrom(clicked));
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
                _hud.SetPrompt(MatchText.AttackPrompt);
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
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return false;

            Vector2 position = pointer.position.ReadValue();
            return !_hud.IsPointerOverUi(position) && _board.TryPick(position, out territory);
        }

        private void HandleRoundStarted(int round, int destinyFace)
        {
            _pendingRoundLine = MatchText.RoundLine(round, destinyFace, _match.RoundOpener);
            string destiny = MatchText.DestinyLine(destinyFace, _match.RoundOpener);
            // Read before the bad omen is applied (it follows this event), so ask Core who it will hit.
            int modifier = Destiny.ReinforcementModifier(_humanGod, destinyFace);
            bool humanLosesTroop =
                Destiny.IsBadOmen(destinyFace)
                && Destiny.BadOmenTarget(_match.Board, _humanGod) != null;
            _destiny.Show(
                round,
                destinyFace,
                destiny,
                _humanGod,
                MatchText.DestinyYouLine(modifier, humanLosesTroop)
            );
        }

        private void HandleTurnStarted(God god, int reinforcements)
        {
            _pendingTurnBanner = god;
            _selected = null;
            _board.ClearHighlights();
            // The last combat stays readable until the next roll or the next turn.
            _dice.Hide();
            if (IsHumanTurn)
            {
                // Cleared, so the previous bot's "X is playing…" never lingers into the human's turn.
                _hud.SetPrompt(string.Empty);
                return;
            }
            _hud.SetPrompt(MatchText.BotPlaying(god));
            // A human hint would otherwise hang over the bot's turn.
            _hud.HideHint();
        }

        private void HandleTerritoryChanged(Territory territory)
        {
            // Only the bad omen changes troops while the Destiny Die rolls; showing it now would spoil the roll.
            if (_destiny.IsPlaying)
            {
                _omenLosses.Add(territory);
                return;
            }
            _board.Refresh(territory);
            _nations.Refresh(_match);
        }

        private void HandleTerritoryCaptured(Territory territory, God newOwner, God? previousOwner)
        {
            if (territory.Kind == TerritoryKind.Temple)
                ShowHintOnce(Hint.Temple);
        }

        private void HandleCombatRolled(Combat combat)
        {
            // The re-roll after an Offering is the moment its troop is paid.
            if (combat.OfferingMade)
                _board.ShowTroopLoss(combat.From);
            _dice.Show(combat);
        }

        // The dice panel is not hidden here, so the verdict stays readable after the troop is lost.
        private void HandleCombatResolved(Combat combat)
        {
            bool attackerWon = combat.Result.Winner == CombatSide.Attacker;
            if ((attackerWon ? combat.Attacker : combat.Defender) == _humanGod)
                _combatsWonByHuman++;

            // A defender at 0 is about to be captured: the change of owner already says it lost,
            // and a "-1" over the attacker's freshly moved-in troops would read as a loss.
            if (attackerWon && combat.To.Troops == 0)
                return;
            _board.ShowTroopLoss(attackerWon ? combat.To : combat.From);
        }

        private void HandleNationEliminated(God god)
        {
            _nations.Refresh(_match);
            if (_botPlaysHuman || god != _humanGod)
                return;

            _humanEliminated = true;
            // No winner yet: the bots could keep playing, so the banner stays neutral.
            ShowEndBanner(MatchText.LoseTitle, MatchText.EliminatedLine(god), null);
        }

        // When the human's elimination leaves one nation, this follows HandleNationEliminated and
        // the banner names that winner instead; it is still a defeat for the human.
        private void HandleMatchWon(God winner, VictoryReason reason) =>
            ShowEndBanner(
                MatchText.EndTitle(winner == _humanGod),
                MatchText.VictoryLine(reason, _match.RoundLimitDecider),
                winner
            );

        private void ShowEndBanner(string title, string detail, God? winner)
        {
            // A temple win at a round's first turn ends the match while the Destiny Die still rolls.
            _destiny.Hide();
            ShowOmenLosses();
            _board.ClearHighlights();
            _nations.Refresh(_match);
            // A temple win at a round's first turn ends the match before the loop shows the line.
            ApplyPendingRoundLine();
            _hud.ShowEndTurn(false);
            _hud.ShowCombatChoice(false);
            _hud.SetPrompt(string.Empty);
            int winnerTemples = winner.HasValue ? _match.Board.TempleCount(winner.Value) : 0;
            _hud.ShowEndBanner(
                title,
                detail,
                winner,
                _match.Round,
                winnerTemples,
                _combatsWonByHuman
            );
        }

        private void HandleGodChosen(God god) => _chosenGod = god;

        private void HandleOfferingClicked() => _offeringRequested = true;

        private void HandleAcceptClicked() => _acceptRequested = true;

        private void HandleEndTurnClicked() => _endTurnRequested = true;

        private void HandleFastBotsClicked()
        {
            MatchSession.FastBots = !MatchSession.FastBots;
            _hud.SetFastBots(MatchSession.FastBots);
        }

        // Restarting is a scene reload: Match, board and views are rebuilt from scratch, so no
        // state can leak between matches. Only the chosen god crosses over, through MatchSession.
        private void HandlePlayAgainClicked()
        {
            MatchSession.ReplayGod = _humanGod;
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
