using System.Collections.Generic;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // Who opens each round. A god's Destiny face lets that god open; 3 and 4 rotate the opener one
    // seat from the previous round (Zeus in round 1). The base cycle Zeus, Ra, Thor, Morrigan follows.
    public sealed class MatchTurnOrderTests
    {
        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        [TestCase(1, new[] { God.Zeus, God.Ra, God.Thor, God.Morrigan })]
        [TestCase(2, new[] { God.Ra, God.Thor, God.Morrigan, God.Zeus })]
        [TestCase(6, new[] { God.Thor, God.Morrigan, God.Zeus, God.Ra })]
        [TestCase(5, new[] { God.Morrigan, God.Zeus, God.Ra, God.Thor })]
        public void GodFace_ThatGodOpensAndTheBaseCycleFollows(int face, God[] expectedTurns)
        {
            Match match = MatchSetup.Start(_board, face, Destiny.GoodFortuneFace);

            Assert.AreEqual(expectedTurns[0], match.RoundOpener);
            CollectionAssert.AreEqual(expectedTurns, PlayRound(match));
            Assert.AreEqual(2, match.Round);
        }

        [TestCase(Destiny.GoodFortuneFace)]
        [TestCase(Destiny.BadOmenFace)]
        public void RoundOne_OnAFaceForEveryone_ZeusOpens(int face)
        {
            Match match = MatchSetup.Start(_board, face);

            Assert.AreEqual(God.Zeus, match.RoundOpener);
            Assert.AreEqual(God.Zeus, match.CurrentGod);
        }

        [Test]
        public void FacesForEveryone_RotateTheOpenerOneSeatFromThePreviousRound()
        {
            // Ra opens by his face, then 3, 4, 3 rotate: Thor, Morrigan, and back around to Zeus.
            Match match = MatchSetup.Start(
                _board,
                Pantheon.SacredFace(God.Ra),
                Destiny.GoodFortuneFace,
                Destiny.BadOmenFace,
                Destiny.GoodFortuneFace
            );
            var openers = new List<God> { match.RoundOpener };

            for (int round = 2; round <= 4; round++)
            {
                PlayRound(match);
                Assert.AreEqual(round, match.Round);
                openers.Add(match.RoundOpener);
            }

            CollectionAssert.AreEqual(new[] { God.Ra, God.Thor, God.Morrigan, God.Zeus }, openers);
        }

        [Test]
        public void GodFaceAfterARotation_StillLetsThatGodOpen()
        {
            Match match = MatchSetup.Start(
                _board,
                Destiny.GoodFortuneFace,
                Pantheon.SacredFace(God.Morrigan)
            );

            PlayRound(match);

            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(God.Morrigan, match.RoundOpener);
            Assert.AreEqual(God.Morrigan, match.CurrentGod);
        }

        [Test]
        public void RoundEnds_AfterTheLastGodBeforeTheOpener()
        {
            Match match = MatchSetup.Start(
                _board,
                Pantheon.SacredFace(God.Thor),
                Destiny.GoodFortuneFace
            );

            for (int i = 0; i < 3; i++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(1, match.Round);
            Assert.AreEqual(God.Ra, match.CurrentGod);

            MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(God.Morrigan, match.RoundOpener); // rotated one seat from Thor
        }

        [Test]
        public void EliminatedGod_IsSkippedAndTheRoundIsShorter()
        {
            MatchSetup.RemoveNation(_board, God.Morrigan);
            Match match = MatchSetup.Start(
                _board,
                Pantheon.SacredFace(God.Thor),
                Destiny.GoodFortuneFace
            );

            List<God> turns = PlayRound(match);

            CollectionAssert.AreEqual(new[] { God.Thor, God.Zeus, God.Ra }, turns);
            Assert.AreEqual(2, match.Round);
        }

        [Test]
        public void RotationOntoAnEliminatedGod_TheNextLivingGodOpens()
        {
            MatchSetup.RemoveNation(_board, God.Ra);
            Match match = MatchSetup.Start(
                _board,
                Destiny.GoodFortuneFace,
                Destiny.GoodFortuneFace
            );

            PlayRound(match);

            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(God.Thor, match.RoundOpener);
        }

        [Test]
        public void OpenerEliminatedMidRound_RoundStillEndsAfterEveryLivingGodPlayed()
        {
            // Ra opens; during Thor's turn Ra loses everything. Morrigan and Zeus still play, then
            // the round ends instead of skipping Ra's seat into a second lap.
            Match match = MatchSetup.Start(
                _board,
                Pantheon.SacredFace(God.Ra),
                Destiny.GoodFortuneFace
            );
            var turns = new List<God> { match.CurrentGod };
            match.TurnStarted += (god, _) => turns.Add(god);

            MatchSetup.PlayPassiveTurn(match);
            MatchSetup.RemoveNation(_board, God.Ra);
            for (int i = 0; i < 3; i++)
                MatchSetup.PlayPassiveTurn(match);

            CollectionAssert.AreEqual(
                new[] { God.Ra, God.Thor, God.Morrigan, God.Zeus, God.Thor },
                turns
            );
            Assert.AreEqual(2, match.Round);
            Assert.AreEqual(God.Thor, match.RoundOpener); // rotated from Ra's seat
        }

        [Test]
        public void RoundStarted_FiresAfterTheOpenerIsDecided()
        {
            var match = new Match(_board, new FakeRandom(Pantheon.SacredFace(God.Morrigan)));
            God? openerSeen = null;
            God? currentSeen = null;
            match.RoundStarted += (round, face) =>
            {
                openerSeen = match.RoundOpener;
                currentSeen = match.CurrentGod;
            };

            match.Start();

            Assert.AreEqual(God.Morrigan, openerSeen);
            Assert.AreEqual(God.Morrigan, currentSeen);
        }

        [Test]
        public void SeededMatch_EveryRoundOpenerFollowsTheDestinyRule()
        {
            var match = new Match(Board.CreateStandard(), new SystemRandom(2026));
            God? previousOpener = null;
            int roundsChecked = 0;
            match.RoundStarted += (round, face) =>
            {
                God? favored = Pantheon.GodOfFace(face);
                God expected =
                    favored
                    ?? (
                        previousOpener == null
                            ? God.Zeus
                            : Match.BaseTurnOrder[
                                (Match.BaseTurnIndex(previousOpener.Value) + 1)
                                    % Match.BaseTurnOrder.Count
                            ]
                    );
                Assert.AreEqual(expected, match.RoundOpener, $"round {round}, face {face}");
                previousOpener = match.RoundOpener;
                roundsChecked++;
            };

            match.Start();
            while (match.Phase != TurnPhase.MatchOver)
                MatchSetup.PlayPassiveTurn(match);

            // Passive play eliminates nobody, so the match runs to the round limit.
            Assert.AreEqual(Match.RoundLimit, roundsChecked);
            Assert.AreEqual(VictoryReason.RoundLimit, match.WinReason);
        }

        // Plays passive turns until a new round starts (or the match ends); returns who played.
        private static List<God> PlayRound(Match match)
        {
            int round = match.Round;
            var turns = new List<God>();
            while (match.Round == round && match.Phase != TurnPhase.MatchOver)
            {
                turns.Add(match.CurrentGod);
                MatchSetup.PlayPassiveTurn(match);
            }
            return turns;
        }
    }
}
