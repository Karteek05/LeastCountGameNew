using System;
using System.Collections.Generic;
using System.Linq;

namespace LeastCount.Core
{
    /// <summary>Per-player result of a declared round.</summary>
    public readonly struct RoundScore
    {
        public int Seat { get; }
        /// <summary>Raw joker-aware hand value (before declare adjustments).</summary>
        public int HandValue { get; }
        /// <summary>Points actually added to this player's running total this round.</summary>
        public int Points { get; }
        public bool IsDeclarer { get; }
        /// <summary>True for the lowest hand(s) that are zeroed out.</summary>
        public bool IsLowest { get; }

        public RoundScore(int seat, int handValue, int points, bool isDeclarer, bool isLowest)
        {
            Seat = seat;
            HandValue = handValue;
            Points = points;
            IsDeclarer = isDeclarer;
            IsLowest = isLowest;
        }
    }

    public static class DeclareScoring
    {
        /// <summary>Wrong-declare penalty added to the declarer's own hand value.</summary>
        public const int WrongDeclarePenalty = 20;

        /// <summary>
        /// Scores a declared round.
        ///  - Declarer lowest (strictly or tied): declarer scores 0.
        ///  - Any player strictly lower than the declarer: declarer scores 20 + own hand value.
        ///  - Every player tied for the lowest hand scores 0.
        ///  - Everyone else scores their hand value.
        /// </summary>
        /// <param name="hands">Each seat's cards. Every participating seat must be present.</param>
        /// <param name="declarerSeat">Seat that declared.</param>
        /// <param name="jokerRank">Round's cut-joker rank (scores 0).</param>
        public static IReadOnlyList<RoundScore> Score(
            IReadOnlyDictionary<int, IReadOnlyList<Card>> hands,
            int declarerSeat,
            Rank jokerRank)
        {
            if (hands == null) throw new ArgumentNullException(nameof(hands));
            if (!hands.ContainsKey(declarerSeat))
                throw new ArgumentException("Declarer seat is not among the hands.", nameof(declarerSeat));

            var handValues = hands.ToDictionary(kv => kv.Key, kv => Scoring.HandValue(kv.Value, jokerRank));
            int lowest = handValues.Values.Min();
            int declarerValue = handValues[declarerSeat];

            // Declarer is "caught" only if someone else is STRICTLY lower than them.
            bool declarerCaught = handValues.Any(kv => kv.Key != declarerSeat && kv.Value < declarerValue);

            var results = new List<RoundScore>(handValues.Count);
            foreach (var kv in handValues.OrderBy(kv => kv.Key))
            {
                int seat = kv.Key;
                int value = kv.Value;
                bool isDeclarer = seat == declarerSeat;
                bool isLowest = value == lowest;
                int points;

                if (isDeclarer)
                    points = declarerCaught ? WrongDeclarePenalty + value : 0;
                else
                    points = isLowest ? 0 : value;

                results.Add(new RoundScore(seat, value, points, isDeclarer, isLowest));
            }
            return results;
        }
    }
}
