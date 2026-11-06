namespace GameJam.Gameplay
{
    public enum TerritoryHighlight
    {
        None,

        /// <summary>The territory the attack would come from.</summary>
        Selected,

        /// <summary>A legal target of the selected territory.</summary>
        Target,
    }
}
