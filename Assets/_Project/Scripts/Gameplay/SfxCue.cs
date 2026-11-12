namespace GameJam.Gameplay
{
    /// <summary>
    /// Every one-shot moment of the match that has a sound. <see cref="AudioCues"/> maps each one to its clips.
    /// Dice rolls are not here: they all share one clip, see <see cref="SfxPlayer.PlayRoll"/>.
    /// </summary>
    public enum SfxCue
    {
        Sacred,
        Cursed,
        TroopPlaced,
        Capture,
        TurnChange,
        Victory,
        Defeat,
        Click,
        Invalid,
        DayZeus,
        DayRa,
        DayThor,
        DayMorrigan,
        BadOmen,
        GoodFortune,
        WrathZeus,
        WrathThor,
        WrathRa,
        WrathMorrigan,
        CardHover,
        BotResult,
        SoftGood,
        SoftBad,
        NeutralWin,
        NeutralLose,
    }
}
