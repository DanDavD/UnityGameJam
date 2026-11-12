using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// The player choices that must survive the scene reload that restarts a match:
    /// the human's god when "Play again" replays, whether the god powers are
    /// on, whether bots play fast and the music and effects volumes.
    /// Reloading the scene rebuilds the Match, board and HUD from scratch, so these are the only
    /// state carried over; a static class is enough and needs no persistent GameObject.
    /// </summary>
    public static class MatchSession
    {
        /// <summary>
        /// Set by "Play again"; when present the menu is skipped and the match starts as this god.
        /// "Main menu" clears it, so the menu opens again.
        /// </summary>
        public static God? ReplayGod { get; set; }

        /// <summary>The menu's "Pantheon powers" (true, the default) or "Classic" choice; Play again keeps it.</summary>
        public static bool PowersEnabled { get; set; } = true;

        public static bool FastBots { get; set; }

        /// <summary>The sound panel's music volume, 0 (silent) to 1.</summary>
        public static float MusicVolume { get; set; } = 1f;

        /// <summary>The sound panel's effects volume (every cue but the music), 0 (silent) to 1.</summary>
        public static float EffectsVolume { get; set; } = 1f;

        // Domain reload is disabled in Enter Play Mode Options, so statics must be reset by hand.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            ReplayGod = null;
            PowersEnabled = true;
            FastBots = false;
            MusicVolume = 1f;
            EffectsVolume = 1f;
        }
    }
}
