using System.Collections.Generic;
using GameJam.Core;
using NUnit.Framework;

namespace GameJam.Tests
{
    // The Day of the God: when the Destiny Die shows a god's face (powers on), that god's Day lasts
    // the round. Storm and Sun at noon resolve at round start; Ragnarök and Night of crows change
    // the round's combats. Zeus 1/6, Thor 6/1, Ra 2/5, Morrigan 5/2 (sacred/cursed).
    public sealed class MatchDayTests
    {
        private const int ZeusFace = 1;
        private const int ThorFace = 6;
        private const int RaFace = 2;
        private const int MorriganFace = 5;

        private Board _board;

        [SetUp]
        public void SetUp()
        {
            _board = Board.CreateStandard();
        }

        [Test]
        public void Storm_EveryEnemyLosesOneButNeverTheLastAndTheTroopsJoinZeus()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Thor, 1); // too small
            Match match = Create(ZeusFace);
            God? day = null;
            IReadOnlyList<Territory> struck = null;
            match.DayStarted += (god, territories) =>
            {
                day = god;
                struck = territories;
            };

            match.Start();

            Assert.AreEqual(God.Zeus, match.DayGod);
            Assert.AreEqual(God.Zeus, day);
            Assert.AreEqual(6, struck.Count); // the six enemy Capitals and Villages
            Assert.AreEqual(2, _board.Get(Board.RaCapital).Troops);
            Assert.AreEqual(1, _board.Get(Board.ThorVillage).Troops);
            Assert.AreEqual(1, _board.Get(Board.EastTemple).Troops);
            Assert.AreEqual(3 + 6, _board.Get(Board.ZeusCapital).Troops);
            Assert.AreEqual(2, _board.Get(Board.ZeusVillage).Troops);
        }

        [Test]
        public void SunAtNoon_ReplacesRasFavoredBonusButHisRivalIsStillWeakened()
        {
            Match match = MatchSetup.StartWithPowers(_board, RaFace);

            Assert.AreEqual(God.Ra, match.CurrentGod);
            Assert.AreEqual(0, match.TurnReinforcements.FromDestiny);
            Assert.AreEqual(3, match.ReinforcementsLeft); // 2 + 2/2, no +2

            MatchSetup.PassUntil(match, God.Morrigan);
            Assert.AreEqual(-Destiny.WeakenedPenalty, match.TurnReinforcements.FromDestiny);
        }

        [Test]
        public void DestinyModifierOf_OnRasDay_MatchesTheReinforcementsEachGodGets()
        {
            Match match = MatchSetup.StartWithPowers(_board, RaFace);

            Assert.AreEqual(0, match.DestinyModifierOf(God.Ra));
            Assert.AreEqual(-Destiny.WeakenedPenalty, match.DestinyModifierOf(God.Morrigan));
            Assert.AreEqual(0, match.DestinyModifierOf(God.Zeus));
            Assert.AreEqual(match.TurnReinforcements.FromDestiny, match.DestinyModifierOf(God.Ra));
        }

        [Test]
        public void DestinyModifierOf_PowersOff_IsTheDestinyRule()
        {
            Match match = MatchSetup.Start(_board, RaFace);

            Assert.AreEqual(Destiny.FavoredBonus, match.DestinyModifierOf(God.Ra));
            Assert.AreEqual(-Destiny.WeakenedPenalty, match.DestinyModifierOf(God.Morrigan));
        }

        [Test]
        public void PowersOff_RaKeepsHisFavoredBonus()
        {
            Match match = MatchSetup.Start(_board, RaFace);

            Assert.AreEqual(Destiny.FavoredBonus, match.TurnReinforcements.FromDestiny);
        }

        [Test]
        public void Storm_ResolvesBeforeTheOpenersTurn()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 3);
            Match match = Create(ZeusFace);
            int northTroopsWhenTurnStarted = -1;
            match.TurnStarted += (_, _) =>
                northTroopsWhenTurnStarted = _board.Get(Board.NorthTemple).Troops;

            match.Start();

            Assert.AreEqual(2, northTroopsWhenTurnStarted);
            Assert.AreEqual(God.Zeus, match.CurrentGod);
            Assert.AreEqual(5, match.ReinforcementsLeft); // 2 + 2/2 + favored 2: the Day adds to it
        }

        [Test]
        public void SunAtNoon_EveryRaTerritoryGainsOne()
        {
            Match match = Create(RaFace);
            IReadOnlyList<Territory> lit = null;
            match.DayStarted += (_, territories) => lit = territories;

            match.Start();

            Assert.AreEqual(God.Ra, match.DayGod);
            CollectionAssert.AreEqual(
                new[] { _board.Get(Board.RaCapital), _board.Get(Board.RaVillage) },
                lit
            );
            Assert.AreEqual(4, _board.Get(Board.RaCapital).Troops);
            Assert.AreEqual(3, _board.Get(Board.RaVillage).Troops);
            Assert.AreEqual(3, _board.Get(Board.ZeusCapital).Troops);
        }

        [Test]
        public void LastingDays_AnnounceNoTerritories()
        {
            Match match = Create(ThorFace);
            IReadOnlyList<Territory> affected = null;
            match.DayStarted += (_, territories) => affected = territories;

            match.Start();

            Assert.IsTrue(match.IsRagnarokActive);
            Assert.IsFalse(match.IsNightOfCrowsActive);
            CollectionAssert.IsEmpty(affected);
        }

        [TestCase(Destiny.GoodFortuneFace)]
        [TestCase(Destiny.BadOmenFace)]
        public void FacesForEveryone_BringNoDay(int face)
        {
            Match match = Create(face);
            int days = 0;
            match.DayStarted += (_, _) => days++;

            match.Start();

            Assert.IsNull(match.DayGod);
            Assert.AreEqual(0, days);
        }

        [Test]
        public void EliminatedGod_HasNoDay()
        {
            MatchSetup.RemoveNation(_board, God.Zeus);
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 3);
            Match match = Create(ZeusFace);
            int days = 0;
            match.DayStarted += (_, _) => days++;

            match.Start();

            Assert.IsNull(match.DayGod);
            Assert.AreEqual(0, days);
            Assert.AreEqual(3, _board.Get(Board.NorthTemple).Troops);
        }

        [Test]
        public void Day_LastsOnlyItsRound()
        {
            Match match = MatchSetup.StartWithPowers(_board, ThorFace, Destiny.GoodFortuneFace);
            Assert.AreEqual(God.Thor, match.DayGod);

            for (int turn = 0; turn < Match.BaseTurnOrder.Count; turn++)
                MatchSetup.PlayPassiveTurn(match);

            Assert.AreEqual(2, match.Round);
            Assert.IsNull(match.DayGod);
            Assert.IsFalse(match.IsRagnarokActive);
        }

        [Test]
        public void PowersOff_GodFacesBringNoDay()
        {
            Match match = MatchSetup.Start(_board, RaFace);

            Assert.IsFalse(match.PowersEnabled);
            Assert.IsNull(match.DayGod);
            Assert.AreEqual(3, _board.Get(Board.RaCapital).Troops);
        }

        // Ragnarök. Thor opens on his face with 5 reinforcements, all on Thor Village (2 + 5 = 7),
        // which attacks Ra on the East Temple.

        [Test]
        public void Ragnarok_ThorAttackingWinsTheTieAndTheDefenderLosesTwo()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 3);
            Match match = StartThorAttack(3, 3);

            CombatResult result = match.PendingCombat.Result;
            Assert.AreEqual(CombatSide.Attacker, result.Winner);
            Assert.AreEqual(CombatReason.RagnarokTie, result.Reason);
            Assert.AreEqual(GodPowers.RagnarokLosses, result.LoserLosses);
            // Thor is winning, so the Offering ("only while losing") follows the new result.
            Assert.IsFalse(match.CanMakeOffering);

            match.ConfirmCombat();

            Assert.AreEqual(1, _board.Get(Board.EastTemple).Troops);
            Assert.AreEqual(God.Ra, _board.Get(Board.EastTemple).Owner);
            Assert.AreEqual(7, _board.Get(Board.ThorVillage).Troops);
        }

        [Test]
        public void Ragnarok_DefenderWithOneTroop_LosesOnlyThatAndIsCaptured()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 1);
            Match match = StartThorAttack(ThorFace, 3);

            Assert.AreEqual(1, match.PendingCombat.Result.LoserLosses);
            match.ConfirmCombat();

            Assert.AreEqual(God.Thor, _board.Get(Board.EastTemple).Owner);
            Assert.AreEqual(6, _board.Get(Board.EastTemple).Troops);
            Assert.AreEqual(1, _board.Get(Board.ThorVillage).Troops);
        }

        [Test]
        public void Ragnarok_ThorLosing_LosesOnlyOne()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 3);
            Match match = StartThorAttack(1, 3); // Thor's cursed 1

            Assert.AreEqual(CombatSide.Defender, match.PendingCombat.Result.Winner);
            Assert.AreEqual(1, match.PendingCombat.Result.LoserLosses);
            match.ConfirmCombat();

            Assert.AreEqual(6, _board.Get(Board.ThorVillage).Troops);
        }

        // Thor plays first, then Morrigan (3 reinforcements on her Village: 5) attacks Thor's South Temple.

        [Test]
        public void Ragnarok_ThorDefendingWinsTheTieAndTheAttackerLosesTwo()
        {
            MatchSetup.Set(_board, Board.SouthTemple, God.Thor, 3);
            Match match = StartMorriganAttackOnThor(3, 3);

            CombatResult result = match.PendingCombat.Result;
            Assert.AreEqual(CombatSide.Defender, result.Winner);
            Assert.AreEqual(CombatReason.RagnarokTie, result.Reason);
            Assert.AreEqual(2, result.LoserLosses);
            match.ConfirmCombat();

            Assert.AreEqual(3, _board.Get(Board.MorriganVillage).Troops);
        }

        [Test]
        public void Ragnarok_AttackingTerritoryAlwaysKeepsOneTroop()
        {
            MatchSetup.Set(_board, Board.SouthTemple, God.Thor, 3);
            Match match = StartMorriganAttackOnThor(3, 3, villageTroops: 2);

            Assert.AreEqual(1, match.PendingCombat.Result.LoserLosses);
            match.ConfirmCombat();

            Assert.AreEqual(1, _board.Get(Board.MorriganVillage).Troops);
            Assert.AreEqual(God.Morrigan, _board.Get(Board.MorriganVillage).Owner);
        }

        [Test]
        public void Ragnarok_OfferingRerollIsResolvedWithRagnarokToo()
        {
            MatchSetup.Set(_board, Board.SouthTemple, God.Thor, 3);
            // Morrigan loses the 3-3 tie to Ragnarök, offers, and re-rolls a neutral 4 that beats 3.
            Match match = StartMorriganAttackOnThor(3, 3, extraRoll: 4);
            Assert.IsTrue(match.CanMakeOffering);

            match.MakeOffering();

            CombatResult result = match.PendingCombat.Result;
            Assert.AreEqual(CombatSide.Attacker, result.Winner);
            Assert.AreEqual(1, result.LoserLosses); // Thor lost, so no Ragnarök bonus
            match.ConfirmCombat();
            Assert.AreEqual(2, _board.Get(Board.SouthTemple).Troops);
            Assert.AreEqual(4, _board.Get(Board.MorriganVillage).Troops);
        }

        [Test]
        public void PowersOff_ThorTie_StillGoesToTheDefender()
        {
            MatchSetup.Set(_board, Board.EastTemple, God.Ra, 3);
            Match match = MatchSetup.Start(_board, ThorFace, 3, 3);
            MatchSetup.PlaceAllOn(match, Board.ThorVillage);
            match.Attack(_board.Get(Board.ThorVillage), _board.Get(Board.EastTemple));

            Assert.AreEqual(CombatReason.TieGoesToDefender, match.PendingCombat.Result.Reason);
            Assert.AreEqual(1, match.PendingCombat.Result.LoserLosses);
        }

        // Night of crows. Morrigan opens on her face and places 5 on her Capital (8), then Zeus
        // (3 reinforcements on his Village: 5) attacks.

        [Test]
        public void NightOfCrows_DefenderLossInAnotherCombat_JoinsMorrigansCapital()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartZeusAttackDuringNightOfCrows(Board.NorthTemple, 1, 3);
            Combat resolved = null;
            match.CombatResolved += combat => resolved = combat;

            match.ConfirmCombat();

            Territory capital = _board.Get(Board.MorriganCapital);
            Assert.AreEqual(1, _board.Get(Board.NorthTemple).Troops);
            Assert.AreEqual(9, capital.Troops);
            Assert.AreEqual(1, resolved.CrowTroops);
            Assert.AreSame(capital, resolved.CrowTerritory);
        }

        [Test]
        public void NightOfCrows_AttackerLoss_AlsoJoinsMorrigan()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            Match match = StartZeusAttackDuringNightOfCrows(Board.NorthTemple, 3, 2); // Ra's sacred 2

            match.ConfirmCombat();

            Assert.AreEqual(4, _board.Get(Board.ZeusVillage).Troops);
            Assert.AreEqual(9, _board.Get(Board.MorriganCapital).Troops);
        }

        [Test]
        public void NightOfCrows_MorrigansOwnLoss_DoesNotFeedTheCrows()
        {
            MatchSetup.Set(_board, Board.WestTemple, God.Morrigan, 2);
            Match match = StartZeusAttackDuringNightOfCrows(Board.WestTemple, 1, 3);
            int capitalBefore = _board.Get(Board.MorriganCapital).Troops;

            match.ConfirmCombat();

            Assert.AreEqual(1, _board.Get(Board.WestTemple).Troops);
            Assert.AreEqual(capitalBefore, _board.Get(Board.MorriganCapital).Troops);
        }

        [Test]
        public void NightOfCrows_OfferingLoss_DoesNotFeedTheCrows()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 2);
            // Zeus 3 loses to Ra 4, offers (that troop is not a combat loss), and wins on his sacred 1.
            Match match = StartZeusAttackDuringNightOfCrows(Board.NorthTemple, 3, 4, 1);

            match.MakeOffering();
            Assert.AreEqual(8, _board.Get(Board.MorriganCapital).Troops);
            match.ConfirmCombat();

            // Only Ra's combat loss reached the crows.
            Assert.AreEqual(9, _board.Get(Board.MorriganCapital).Troops);
        }

        [Test]
        public void NightOfCrows_AtMostThreeTroopsPerRound()
        {
            MatchSetup.Set(_board, Board.NorthTemple, God.Ra, 5);
            Match match = StartZeusAttackDuringNightOfCrows(
                Board.NorthTemple,
                1,
                3,
                1,
                3,
                1,
                3,
                1,
                3
            );
            Territory village = _board.Get(Board.ZeusVillage);
            Territory north = _board.Get(Board.NorthTemple);
            match.ConfirmCombat();
            for (int combat = 2; combat <= 3; combat++)
            {
                match.Attack(village, north);
                match.ConfirmCombat();
            }
            Assert.AreEqual(
                8 + GodPowers.NightOfCrowsMaxPerRound,
                _board.Get(Board.MorriganCapital).Troops
            );

            match.Attack(village, north);
            Combat fourth = match.PendingCombat;
            match.ConfirmCombat();

            Assert.AreEqual(1, north.Troops);
            Assert.AreEqual(11, _board.Get(Board.MorriganCapital).Troops);
            Assert.AreEqual(0, fourth.CrowTroops);
            Assert.IsNull(fourth.CrowTerritory);
        }

        [Test]
        public void NightOfCrows_LandingOnTheAttackingTerritoryOfACapture_IsReportedOnce()
        {
            // Without her Capital, the crows go to Morrígan's largest army: the attacking Village (6).
            MatchSetup.Set(_board, Board.MorriganCapital, God.Thor, 1);
            MatchSetup.Set(_board, Board.WestTemple, God.Zeus, 1);
            Match match = Create(MorriganFace, 5, 3);
            match.Start();
            MatchSetup.PlaceAllOn(match, Board.MorriganVillage);
            Territory village = _board.Get(Board.MorriganVillage);
            match.Attack(village, _board.Get(Board.WestTemple));
            Combat combat = match.PendingCombat;
            int villageReports = 0;
            match.TerritoryChanged += territory =>
            {
                if (territory == village)
                    villageReports++;
            };

            match.ConfirmCombat();

            Assert.AreSame(village, combat.CrowTerritory);
            Assert.AreEqual(1, combat.CrowTroops);
            Assert.AreEqual(1, villageReports);
            Assert.AreEqual(6, _board.Get(Board.WestTemple).Troops); // 6 + 1 crow, all but one move
            Assert.AreEqual(1, village.Troops);
        }

        private Match Create(int destinyFace, params int[] combatRolls)
        {
            var rolls = new int[combatRolls.Length + 1];
            rolls[0] = destinyFace;
            combatRolls.CopyTo(rolls, 1);
            return new Match(_board, new FakeRandom(rolls), powersEnabled: true);
        }

        private Match StartThorAttack(int thorRoll, int raRoll)
        {
            Match match = Create(ThorFace, thorRoll, raRoll);
            match.Start();
            MatchSetup.PlaceAllOn(match, Board.ThorVillage);
            match.Attack(_board.Get(Board.ThorVillage), _board.Get(Board.EastTemple));
            return match;
        }

        private Match StartMorriganAttackOnThor(
            int morriganRoll,
            int thorRoll,
            int villageTroops = 0,
            int extraRoll = 0
        )
        {
            Match match =
                extraRoll == 0
                    ? Create(ThorFace, morriganRoll, thorRoll)
                    : Create(ThorFace, morriganRoll, thorRoll, extraRoll);
            match.Start();
            MatchSetup.PassUntil(match, God.Morrigan);
            MatchSetup.PlaceAllOn(match, Board.MorriganVillage);
            if (villageTroops > 0)
                MatchSetup.Set(_board, Board.MorriganVillage, God.Morrigan, villageTroops);
            match.Attack(_board.Get(Board.MorriganVillage), _board.Get(Board.SouthTemple));
            return match;
        }

        private Match StartZeusAttackDuringNightOfCrows(int targetId, params int[] combatRolls)
        {
            Match match = Create(MorriganFace, combatRolls);
            match.Start();
            Assert.IsTrue(match.IsNightOfCrowsActive);
            MatchSetup.PlaceAllOn(match, Board.MorriganCapital);
            match.EndTurn();
            MatchSetup.PlaceAllOn(match, Board.ZeusVillage);
            match.Attack(_board.Get(Board.ZeusVillage), _board.Get(targetId));
            return match;
        }
    }
}
