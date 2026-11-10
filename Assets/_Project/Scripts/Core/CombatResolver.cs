using System;

namespace GameJam.Core
{
    public static class CombatResolver
    {
        /// <summary>The loser of a combat loses one troop, unless a god power says otherwise.</summary>
        public const int TroopsLostPerCombat = 1;

        public static CombatResult Resolve(
            God attacker,
            int attackerRoll,
            God defender,
            int defenderRoll
        ) => Resolve(attacker, attackerRoll, defender, defenderRoll, ragnarok: false);

        /// <summary>
        /// <paramref name="ragnarok"/> is true on Thor's Day: if Thor is in this combat he wins ties
        /// and, when he wins, the loser loses <see cref="GodPowers.RagnarokLosses"/>.
        /// </summary>
        public static CombatResult Resolve(
            God attacker,
            int attackerRoll,
            God defender,
            int defenderRoll,
            bool ragnarok
        )
        {
            if (attacker == defender)
                throw new ArgumentException("A god cannot attack itself.", nameof(defender));

            // Read validates both faces.
            RollOutcome attackerOutcome = Pantheon.Read(attacker, attackerRoll);
            RollOutcome defenderOutcome = Pantheon.Read(defender, defenderRoll);
            bool thorRages = ragnarok && (attacker == God.Thor || defender == God.Thor);

            CombatSide winner;
            CombatReason reason;
            if (attackerOutcome != defenderOutcome)
            {
                winner =
                    attackerOutcome > defenderOutcome ? CombatSide.Attacker : CombatSide.Defender;
                reason = CombatReason.BetterOmen;
            }
            else if (attackerOutcome == RollOutcome.Neutral && attackerRoll != defenderRoll)
            {
                winner = attackerRoll > defenderRoll ? CombatSide.Attacker : CombatSide.Defender;
                reason = CombatReason.HigherRoll;
            }
            else if (thorRages)
            {
                // Reported as Ragnarök even when Thor defends, so the UI always names the rule in play.
                winner = attacker == God.Thor ? CombatSide.Attacker : CombatSide.Defender;
                reason = CombatReason.RagnarokTie;
            }
            else
            {
                winner = CombatSide.Defender;
                reason = CombatReason.TieGoesToDefender;
            }

            God winnerGod = winner == CombatSide.Attacker ? attacker : defender;
            int losses =
                thorRages && winnerGod == God.Thor ? GodPowers.RagnarokLosses : TroopsLostPerCombat;

            return new CombatResult(
                winner,
                reason,
                attackerRoll,
                defenderRoll,
                attackerOutcome,
                defenderOutcome,
                losses
            );
        }

        public static CombatResult Roll(God attacker, God defender, IRandom random)
        {
            int attackerRoll = Die.Roll(random);
            int defenderRoll = Die.Roll(random);
            return Resolve(attacker, attackerRoll, defender, defenderRoll);
        }
    }
}
