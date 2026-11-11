using System;
using System.Collections.Generic;

namespace GameJam.Core
{
    /// <summary>
    /// The dice draw that fixes the turn order for a whole match. Round 1: Zeus vs Thor and Ra vs
    /// Morrígan (rivals, so each duel is a clash of sacred and cursed numbers). Round 2: the two
    /// winners duel for 1st and 2nd, the two losers for 3rd and 4th. Ties roll again.
    /// </summary>
    public sealed class TurnOrderDraw
    {
        /// <summary>The four duels in play order: Zeus-Thor, Ra-Morrígan, winners, losers.</summary>
        public IReadOnlyList<DrawDuel> Duels { get; }

        /// <summary>1st to 4th: the order every round of the match follows.</summary>
        public IReadOnlyList<God> Order { get; }

        private TurnOrderDraw(IReadOnlyList<DrawDuel> duels, IReadOnlyList<God> order)
        {
            Duels = duels;
            Order = order;
        }

        public static TurnOrderDraw Draw(IRandom random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            DrawDuel zeusThor = Duel(random, God.Zeus, God.Thor);
            DrawDuel raMorrigan = Duel(random, God.Ra, God.Morrigan);
            DrawDuel winners = Duel(random, zeusThor.Winner, raMorrigan.Winner);
            DrawDuel losers = Duel(random, zeusThor.Loser, raMorrigan.Loser);

            return new TurnOrderDraw(
                new[] { zeusThor, raMorrigan, winners, losers },
                new[] { winners.Winner, winners.Loser, losers.Winner, losers.Loser }
            );
        }

        private static DrawDuel Duel(IRandom random, God first, God second)
        {
            var attempts = new List<DrawAttempt>();
            DrawAttempt attempt;
            do
            {
                attempt = new DrawAttempt(first, Die.Roll(random), second, Die.Roll(random));
                attempts.Add(attempt);
            } while (attempt.IsTie);
            return new DrawDuel(first, second, attempts);
        }
    }
}
