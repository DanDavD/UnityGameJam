using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// The two player choices that must survive the scene reload that restarts a match:
    /// which god "Play again" replays and whether bots play fast.
    /// Reloading the scene rebuilds the Match, board and HUD from scratch, so these are the only
    /// state carried over; a static class is enough and needs no persistent GameObject.
    /// </summary>
    public static class MatchSession
    {
        /// <summary>Set by "Play again"; when present the menu is skipped and the match starts with it.</summary>
        public static God? ReplayGod { get; set; }

        public static bool FastBots { get; set; }

        // Domain reload is disabled in Enter Play Mode Options, so statics must be reset by hand.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            ReplayGod = null;
            FastBots = false;
        }
    }
}
