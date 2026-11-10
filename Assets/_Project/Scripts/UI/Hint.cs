namespace GameJam.UI
{
    /// <summary>The situations that get a one-time onboarding hint the first time they happen in a match.</summary>
    public enum Hint
    {
        Destiny,
        Reinforce,
        Attack,
        Combat,
        Offering,
        Temple,

        /// <summary>The first round with a Day of the God (powers on).</summary>
        Day,

        /// <summary>The first time the human's Favor changes (powers on).</summary>
        Favor,

        /// <summary>The first time the human's Favor is full and the Wrath can be unleashed.</summary>
        Wrath,
    }
}
