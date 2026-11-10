namespace GameJam.Core
{
    public enum CombatReason
    {
        /// <summary>The winner's roll outcome ranked higher: Sacred beats Neutral beats Cursed.</summary>
        BetterOmen,

        /// <summary>Both rolls were neutral and the higher number won.</summary>
        HigherRoll,

        /// <summary>Same outcome and no higher number decides, so the defender holds.</summary>
        TieGoesToDefender,

        /// <summary>A tie during Ragnarök (Thor's Day): Thor wins it, attacking or defending.</summary>
        RagnarokTie,
    }
}
