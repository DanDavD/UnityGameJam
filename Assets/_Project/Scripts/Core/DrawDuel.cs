using System.Collections.Generic;

namespace GameJam.Core
{
    /// <summary>A duel of the turn-order draw: every attempt, the last one deciding it.</summary>
    public sealed class DrawDuel
    {
        public God First { get; }
        public God Second { get; }
        public IReadOnlyList<DrawAttempt> Attempts { get; }

        public God Winner => Attempts[Attempts.Count - 1].Winner.Value;
        public God Loser => Winner == First ? Second : First;

        public DrawDuel(God first, God second, IReadOnlyList<DrawAttempt> attempts)
        {
            First = first;
            Second = second;
            Attempts = attempts;
        }
    }
}
