using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// The player choices that must survive the scene reload that restarts a match:
    /// who sits at each god when "Play again" replays (solo or hot-seat), whether the god powers are
    /// on, whether bots play fast and whether sound is muted.
    /// Reloading the scene rebuilds the Match, board and HUD from scratch, so these are the only
    /// state carried over; a static class is enough and needs no persistent GameObject.
    /// </summary>
    public static class MatchSession
    {
        /// <summary>
        /// Set by "Play again"; when present the menu is skipped and the match starts with these
        /// seats. "Main menu" clears it, so the menu opens on solo again.
        /// </summary>
        public static PlayerSeats ReplaySeats { get; set; }

        /// <summary>The menu's "Pantheon powers" (true, the default) or "Classic" choice; Play again keeps it.</summary>
        public static bool PowersEnabled { get; set; } = true;

        public static bool FastBots { get; set; }

        public static bool Muted { get; set; }

        // Domain reload is disabled in Enter Play Mode Options, so statics must be reset by hand.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            ReplaySeats = null;
            PowersEnabled = true;
            FastBots = false;
            Muted = false;
        }
    }
}
