using System;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // Seats decide who clicks, whose eyes a combat is read through, and when the match is over for
    // the people at the table (solo: the player is out; hot-seat: every person is out).
    public sealed class PlayerSeatsTests
    {
        // Indexed like Match.BaseTurnOrder: Zeus, Ra, Thor, Morrígan.
        private static PlayerSeats Seats(bool zeus, bool ra, bool thor, bool morrigan) =>
            new PlayerSeats(new[] { zeus, ra, thor, morrigan }, botsPlayEverySeat: false);

        private static Match NewMatch() => new Match(Board.CreateStandard(), new FakeRandom());

        [Test]
        public void Solo_OnlyTheChosenGodIsHuman()
        {
            PlayerSeats seats = PlayerSeats.Solo(God.Thor, botsPlayEverySeat: false);

            Assert.IsTrue(seats.IsHuman(God.Thor));
            Assert.IsFalse(seats.IsHuman(God.Zeus));
            Assert.IsFalse(seats.IsHuman(God.Ra));
            Assert.IsFalse(seats.IsHuman(God.Morrigan));
            Assert.AreEqual(1, seats.PlayerCount);
            Assert.AreEqual(God.Thor, seats.FirstPlayer);
            Assert.IsFalse(seats.IsHotSeat);
        }

        [Test]
        public void TwoOrMorePlayers_IsHotSeat_AndFirstPlayerFollowsTheBaseCycle()
        {
            PlayerSeats seats = Seats(zeus: false, ra: false, thor: true, morrigan: true);

            Assert.IsTrue(seats.IsHotSeat);
            Assert.AreEqual(2, seats.PlayerCount);
            Assert.AreEqual(God.Thor, seats.FirstPlayer);
            Assert.IsTrue(seats.IsHuman(God.Morrigan));
            Assert.IsFalse(seats.IsHuman(God.Ra));
        }

        [Test]
        public void OnePlayerChosenInHotSeat_PlaysLikeSolo()
        {
            PlayerSeats seats = Seats(zeus: false, ra: true, thor: false, morrigan: false);

            Assert.IsFalse(seats.IsHotSeat);
            Assert.AreEqual(God.Ra, seats.FirstPlayer);
        }

        [Test]
        public void IsValid_NeedsFourSeatsAndAtLeastOnePlayer()
        {
            Assert.IsTrue(PlayerSeats.IsValid(new[] { false, false, false, true }));
            Assert.IsFalse(PlayerSeats.IsValid(new[] { false, false, false, false }));
            Assert.IsFalse(PlayerSeats.IsValid(new[] { true, true }));
            Assert.IsFalse(PlayerSeats.IsValid(null));
        }

        [Test]
        public void Constructor_RejectsAMatchWithoutPlayers()
        {
            Assert.Throws<ArgumentException>(() =>
                new PlayerSeats(new bool[Match.BaseTurnOrder.Count], false)
            );
        }

        [Test]
        public void Constructor_CopiesTheSeats_SoLaterMenuClicksCannotChangeAStartedMatch()
        {
            bool[] menu = { true, true, false, false };
            var seats = new PlayerSeats(menu, false);

            menu[1] = false;

            Assert.IsTrue(seats.IsHuman(God.Ra));
        }

        [Test]
        public void BotsPlayEverySeat_NobodyClicks_ButTheSeatsAreRemembered()
        {
            var seats = new PlayerSeats(
                new[] { true, false, true, false },
                botsPlayEverySeat: true
            );
            Match match = NewMatch();
            MatchSetup.RemoveNation(match.Board, God.Zeus);
            MatchSetup.RemoveNation(match.Board, God.Thor);

            Assert.IsFalse(seats.HasHuman);
            Assert.IsFalse(seats.IsHuman(God.Zeus));
            Assert.IsTrue(seats.IsPlayerSeat(God.Zeus));
            // The debug match always plays on to a winner.
            Assert.IsFalse(seats.AreAllHumansOut(match));
        }

        [Test]
        public void PerspectiveOf_APersonAttacking_IsTheAttacker()
        {
            PlayerSeats seats = Seats(zeus: true, ra: true, thor: false, morrigan: false);

            Assert.AreEqual(God.Zeus, seats.PerspectiveOf(God.Zeus, God.Thor));
            // Both are people: it is the attacker's turn, so the attacker is holding the mouse.
            Assert.AreEqual(God.Zeus, seats.PerspectiveOf(God.Zeus, God.Ra));
        }

        [Test]
        public void PerspectiveOf_ABotAttackingAPerson_IsTheDefender()
        {
            PlayerSeats seats = Seats(zeus: true, ra: true, thor: false, morrigan: false);

            Assert.AreEqual(God.Ra, seats.PerspectiveOf(God.Thor, God.Ra));
        }

        [Test]
        public void PerspectiveOf_BotsOnly_IsNobody()
        {
            PlayerSeats seats = Seats(zeus: true, ra: false, thor: false, morrigan: false);

            Assert.IsNull(seats.PerspectiveOf(God.Thor, God.Ra));
            Assert.IsNull(
                new PlayerSeats(new[] { true, false, false, false }, true).PerspectiveOf(
                    God.Zeus,
                    God.Thor
                )
            );
        }

        [Test]
        public void Solo_PlayerEliminated_EndsTheMatchForHumans()
        {
            PlayerSeats seats = PlayerSeats.Solo(God.Ra, false);
            Match match = NewMatch();

            MatchSetup.RemoveNation(match.Board, God.Ra);

            Assert.IsTrue(seats.AreAllHumansOut(match));
            Assert.IsTrue(seats.EndsMatchForHumans(match, God.Ra));
        }

        [Test]
        public void HotSeat_OnePersonOut_TheOthersPlayOn()
        {
            PlayerSeats seats = Seats(zeus: true, ra: false, thor: true, morrigan: false);
            Match match = NewMatch();

            MatchSetup.RemoveNation(match.Board, God.Zeus);

            Assert.IsFalse(seats.AreAllHumansOut(match));
            Assert.IsFalse(seats.EndsMatchForHumans(match, God.Zeus));
        }

        [Test]
        public void HotSeat_EveryPersonOut_EndsTheMatchForHumans()
        {
            PlayerSeats seats = Seats(zeus: true, ra: false, thor: true, morrigan: false);
            Match match = NewMatch();

            MatchSetup.RemoveNation(match.Board, God.Zeus);
            MatchSetup.RemoveNation(match.Board, God.Thor);

            Assert.IsTrue(seats.AreAllHumansOut(match));
            Assert.IsTrue(seats.EndsMatchForHumans(match, God.Thor));
        }

        [Test]
        public void BotEliminated_EndsTheMatchOnlyWhenOneNationIsLeft()
        {
            PlayerSeats seats = Seats(zeus: true, ra: true, thor: false, morrigan: false);
            Match match = NewMatch();

            MatchSetup.RemoveNation(match.Board, God.Thor);
            Assert.IsFalse(seats.EndsMatchForHumans(match, God.Thor));

            MatchSetup.RemoveNation(match.Board, God.Morrigan);
            MatchSetup.RemoveNation(match.Board, God.Ra);
            // Zeus is the last nation standing: his win follows.
            Assert.IsTrue(seats.EndsMatchForHumans(match, God.Morrigan));
        }

        [Test]
        public void EndsMatchForHumans_IsFalseForANationStillOnTheBoard()
        {
            PlayerSeats seats = PlayerSeats.Solo(God.Zeus, false);

            Assert.IsFalse(seats.EndsMatchForHumans(NewMatch(), God.Zeus));
        }
    }
}
