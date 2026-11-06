using System;

namespace GameJam.Core
{
    public static class CombatResolver
    {
        public static CombatResult Resolve(
            God attacker,
            int attackerRoll,
            God defender,
            int defenderRoll
        )
        {
            if (attacker == defender)
                throw new ArgumentException("A god cannot attack itself.", nameof(defender));

            // Read validates both faces.
            RollOutcome attackerOutcome = Pantheon.Read(attacker, attackerRoll);
            RollOutcome defenderOutcome = Pantheon.Read(defender, defenderRoll);

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
            else
            {
                winner = CombatSide.Defender;
                reason = CombatReason.TieGoesToDefender;
            }

            return new CombatResult(
                winner,
                reason,
                attackerRoll,
                defenderRoll,
                attackerOutcome,
                defenderOutcome
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
