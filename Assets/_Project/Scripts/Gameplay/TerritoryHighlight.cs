namespace GameJam.Gameplay
{
    public enum TerritoryHighlight
    {
        None,

        /// <summary>The territory the attack would come from.</summary>
        Selected,

        /// <summary>A legal target of the selected territory.</summary>
        Target,

        /// <summary>Sacred Road mode: an army of yours that can march (thick breathing ring in your color, "ROAD" tag).</summary>
        RoadOrigin,

        /// <summary>Sacred Road mode: where the picked army can march to (thick breathing ring in your color, with the "+N" preview).</summary>
        RoadDestination,
    }
}
