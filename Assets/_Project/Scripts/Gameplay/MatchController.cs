using System;
using System.Collections;
using GameJam.AI;
using GameJam.Core;
using GameJam.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Runs one match in the scene. A single coroutine loop issues every Core command (human
    /// clicks or bot decisions) one step at a time; Core event handlers only update visuals.
    /// Keeping commands out of handlers respects Match's rule that handlers must not re-enter it.
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

        [Header("Players")]
        [SerializeField]
        private God _humanGod = God.Zeus;

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

        private Match _match;
        private Territory _selected;
        private WaitForSeconds _botPlaceWait;
        private WaitForSeconds _botStepWait;
        private WaitForSeconds _resultWait;

        // Set by HUD button events and consumed by the loop, so clicks never issue commands directly.
        private bool _offeringRequested;
        private bool _acceptRequested;
        private bool _endTurnRequested;

        // The human is out: the match is over for them even though the bots could keep playing.
        private bool _humanEliminated;

        private bool IsHumanTurn => !_botPlaysHuman && _match.CurrentGod == _humanGod;

        private void Awake()
        {
            _match = new Match(Board.CreateStandard(), new SystemRandom());
            _board.Build(_match.Board);
            _botPlaceWait = new WaitForSeconds(_botPlacePause);
            _botStepWait = new WaitForSeconds(_botStepPause);
            _resultWait = new WaitForSeconds(_resultHold);
        }

        // Subscribed before Start() so the first RoundStarted and TurnStarted are not missed.
        private void OnEnable()
        {
            _match.RoundStarted += HandleRoundStarted;
            _match.TurnStarted += HandleTurnStarted;
            _match.TerritoryChanged += HandleTerritoryChanged;
            _match.CombatRolled += HandleCombatRolled;
            _match.CombatResolved += HandleCombatResolved;
            _match.NationEliminated += HandleNationEliminated;
            _match.MatchWon += HandleMatchWon;
            _hud.OfferingClicked += HandleOfferingClicked;
            _hud.AcceptClicked += HandleAcceptClicked;
            _hud.EndTurnClicked += HandleEndTurnClicked;
            _hud.PlayAgainClicked += HandlePlayAgainClicked;
        }

        private void OnDisable()
        {
            _match.RoundStarted -= HandleRoundStarted;
            _match.TurnStarted -= HandleTurnStarted;
            _match.TerritoryChanged -= HandleTerritoryChanged;
            _match.CombatRolled -= HandleCombatRolled;
            _match.CombatResolved -= HandleCombatResolved;
            _match.NationEliminated -= HandleNationEliminated;
            _match.MatchWon -= HandleMatchWon;
            _hud.OfferingClicked -= HandleOfferingClicked;
            _hud.AcceptClicked -= HandleAcceptClicked;
            _hud.EndTurnClicked -= HandleEndTurnClicked;
            _hud.PlayAgainClicked -= HandlePlayAgainClicked;
        }

        // In Start, not Awake: HudView queries its elements in its own OnEnable.
        private void Start()
        {
            _hud.SetIdentity(_humanGod, MatchText.IdentityLine(_humanGod));
            StartCoroutine(Run());
        }

        // The turn state machine lives in Match; this loop only asks who acts in the current phase.
        private IEnumerator Run()
        {
            _match.Start();
            // Stopping when the human is eliminated also stops every remaining bot pause.
            while (_match.Phase != TurnPhase.MatchOver && !_humanEliminated)
            {
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

            while (true)
            {
                // Same as HumanReinforce: never read the click that issued the previous command.
                yield return null;
                if (_endTurnRequested)
                {
                    Select(null);
                    _hud.ShowEndTurn(false);
                    _match.EndTurn();
                    yield break;
                }

                if (TryGetClickedTerritory(out Territory clicked))
                {
                    if (_selected != null && _match.CanAttack(_selected, clicked))
                    {
                        _hud.ShowEndTurn(false);
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

            if (IsHumanTurn && _match.CanMakeOffering)
            {
                yield return HumanCombatChoice();
                yield break;
            }

            yield return _resultWait;
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
            yield return _botPlaceWait;
            _match.PlaceTroop(BotPlayer.ChooseReinforcement(_match));
        }

        private IEnumerator BotAttack()
        {
            _board.ClearHighlights();
            yield return _botStepWait;
            if (!BotPlayer.TryChooseAttack(_match, out Territory from, out Territory to))
            {
                _match.EndTurn();
                yield break;
            }

            // Show the intended attack first so the player can follow what the bot does.
            _board.SetHighlight(from, TerritoryHighlight.Selected);
            _board.SetHighlight(to, TerritoryHighlight.Target);
            yield return _botStepWait;
            _match.Attack(from, to);
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

        private void HandleRoundStarted(int round, int destinyFace) =>
            _hud.SetRound(MatchText.RoundLine(round, destinyFace, _match.RoundOpener));

        private void HandleTurnStarted(God god, int reinforcements)
        {
            _selected = null;
            _board.ClearHighlights();
            // The last combat stays readable until the next roll or the next turn.
            _dice.Hide();
            if (!IsHumanTurn)
                _hud.SetPrompt(MatchText.BotPlaying(god));
        }

        private void HandleTerritoryChanged(Territory territory) => _board.Refresh(territory);

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
            // A defender at 0 is about to be captured: the change of owner already says it lost,
            // and a "-1" over the attacker's freshly moved-in troops would read as a loss.
            if (attackerWon && combat.To.Troops == 0)
                return;
            _board.ShowTroopLoss(attackerWon ? combat.To : combat.From);
        }

        private void HandleNationEliminated(God god)
        {
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
                MatchText.VictoryLine(winner, reason, _match.RoundLimitDecider),
                winner
            );

        private void ShowEndBanner(string title, string detail, God? winner)
        {
            _board.ClearHighlights();
            _hud.ShowEndTurn(false);
            _hud.ShowCombatChoice(false);
            _hud.SetPrompt(string.Empty);
            _hud.ShowEndBanner(title, detail, winner);
        }

        private void HandleOfferingClicked() => _offeringRequested = true;

        private void HandleAcceptClicked() => _acceptRequested = true;

        private void HandleEndTurnClicked() => _endTurnRequested = true;

        private void HandlePlayAgainClicked() =>
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
