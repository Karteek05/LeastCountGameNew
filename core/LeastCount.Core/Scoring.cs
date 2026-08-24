using System.Collections.Generic;

namespace LeastCount.Core
{
    /// <summary>
    /// Point values for scoring. This is deliberately separate from <see cref="Card.Rank"/>:
    /// the round's cut-joker rank scores 0, and printed jokers always score 0, so point value
    /// is NOT a static property of a card. Every scoring path must pass through here with the
    /// round's joker rank.
    ///
    /// Fixes two legacy bugs by construction:
    ///  - old code summed raw wire bytes (K♠ and K♥ scored differently);
    ///  - old code had no notion of the joker rank scoring 0.
    /// </summary>
    public static class Scoring
    {
        /// <summary>
        /// Point value of a single card given the round's cut-joker rank.
        /// Ace=1, 2..10 face value, Jack=11, Queen=12, King=13. Printed jokers and any card of
        /// the cut-joker rank score 0.
        /// </summary>
        public static int PointValue(Card card, Rank jokerRank)
        {
            if (card.IsPrintedJoker) return 0;
            if (card.Rank == jokerRank) return 0;
            return (int)card.Rank; // Ace..King already map 1..13
        }

        /// <summary>Sum of point values for a hand, with the round's cut-joker rank scoring 0.</summary>
        public static int HandValue(IEnumerable<Card> hand, Rank jokerRank)
        {
            int total = 0;
            foreach (Card c in hand)
                total += PointValue(c, jokerRank);
            return total;
        }
    }
}
