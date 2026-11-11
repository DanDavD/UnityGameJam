namespace GameJam.UI
{
    /// <summary>What the Wrath button shows: its look and its state line (see MatchText.WrathState).</summary>
    public enum WrathButtonState
    {
        /// <summary>Favor below the maximum, or full but with nothing to strike: framed in the god's color.</summary>
        Locked,

        /// <summary>Favor full and a target exists: solid nation color, halo and breath. The only clickable state.</summary>
        Ready,

        /// <summary>Already unleashed this turn.</summary>
        Used,

        /// <summary>The player is picking its target: the button turns into "cancel".</summary>
        Aiming,
    }
}
