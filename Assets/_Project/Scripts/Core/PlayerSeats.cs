using System;
using System.Collections.Generic;
using GameJam.Core;

namespace GameJam.Core
{
    /// <summary>
    /// Who sits at each god: a person (a player seat) or a bot. Solo is one player seat; hot-seat is
    /// two to four people sharing the screen and mouse. Every "is this god played by a person?"
    /// question goes through here, and so do the two seat rules the presentation needs: whose eyes
    /// a combat is read through, and when the match is over for the people at the table.
    /// The debug "bot plays human" flag hands every player seat to a bot, so a match plays by itself
    /// while the HUD still speaks to the seats chosen in the menu.
    /// Plain C# and immutable, so it can cross the "Play again" scene reload in MatchSession.
    /// </summary>
    public sealed class PlayerSeats
    {
        // Indexed like Match.BaseTurnOrder.
        private readonly bool[] _playerSeats = new bool[Match.BaseTurnOrder.Count];
        private readonly bool _botsPlayEverySeat;

        /// <param name="playerSeats">One entry per god in Match.BaseTurnOrder: true for a person.</param>
        /// <param name="botsPlayEverySeat">Debug: bots also play the player seats.</param>
        public PlayerSeats(IReadOnlyList<bool> playerSeats, bool botsPlayEverySeat)
        {
            if (!IsValid(playerSeats))
                throw new ArgumentException(
                    $"Needs {Match.BaseTurnOrder.Count} seats with at least one player.",
                    nameof(playerSeats)
                );

            God? first = null;
            for (int i = 0; i < _playerSeats.Length; i++)
            {
                _playerSeats[i] = playerSeats[i];
                if (!playerSeats[i])
                    continue;
                PlayerCount++;
                first ??= Match.BaseTurnOrder[i];
            }
            // IsValid guarantees at least one player seat.
            FirstPlayer = first.Value;
            _botsPlayEverySeat = botsPlayEverySeat;
        }

        /// <summary>One person at <paramref name="god"/>, bots at the other three.</summary>
        public static PlayerSeats Solo(God god, bool botsPlayEverySeat)
        {
            var seats = new bool[Match.BaseTurnOrder.Count];
            seats[Match.BaseTurnIndex(god)] = true;
            return new PlayerSeats(seats, botsPlayEverySeat);
        }

        /// <summary>True for one entry per god with at least one person: the menu's START rule.</summary>
        public static bool IsValid(IReadOnlyList<bool> playerSeats)
        {
            if (playerSeats == null || playerSeats.Count != Match.BaseTurnOrder.Count)
                return false;
            for (int i = 0; i < playerSeats.Count; i++)
            {
                if (playerSeats[i])
                    return true;
            }
            return false;
        }

        /// <summary>How many gods the menu gave to people (1 to 4).</summary>
        public int PlayerCount { get; }

        /// <summary>The first player seat in base turn order: the solo player, or hot-seat's first view.</summary>
        public God FirstPlayer { get; }

        /// <summary>Two or more people share the screen: the HUD names gods instead of saying "you".</summary>
        public bool IsHotSeat => PlayerCount > 1;

        /// <summary>False when bots play every god (debug): no clicks are read and no hints show.</summary>
        public bool HasHuman => !_botsPlayEverySeat;

        /// <summary>The menu gave this god to a person, even while the debug bot stands in for them.</summary>
        public bool IsPlayerSeat(God god) => _playerSeats[Match.BaseTurnIndex(god)];

        /// <summary>True when a person plays this god by clicking; false when a bot plays it.</summary>
        public bool IsHuman(God god) => HasHuman && IsPlayerSeat(god);

        /// <summary>
        /// The person a combat is read through (dice sting, Favor news, combat hint): the attacker
        /// when a person attacks (it is their turn), else a person defending, else nobody.
        /// </summary>
        public God? PerspectiveOf(God attacker, God defender)
        {
            if (IsHuman(attacker))
                return attacker;
            if (IsHuman(defender))
                return defender;
            return null;
        }

        /// <summary>
        /// True once every person is eliminated: the match is over for the table even though the
        /// bots could keep playing. Never true when bots play every seat (debug).
        /// </summary>
        public bool AreAllHumansOut(Match match)
        {
            if (!HasHuman)
                return false;
            for (int i = 0; i < _playerSeats.Length; i++)
            {
                if (_playerSeats[i] && !match.IsEliminated(Match.BaseTurnOrder[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// True when <paramref name="eliminated"/> is now out and that ends the match for the
        /// people: no person is left, or only one nation is (a win follows).
        /// </summary>
        public bool EndsMatchForHumans(Match match, God eliminated)
        {
            if (!match.IsEliminated(eliminated))
                return false;
            if (AreAllHumansOut(match))
                return true;

            int living = 0;
            foreach (God god in Match.BaseTurnOrder)
            {
                if (!match.IsEliminated(god))
                    living++;
            }
            return living == 1;
        }
    }
}
