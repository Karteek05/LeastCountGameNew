using LeastCount.Core;

namespace LeastCount.Core.Tests
{
    /// <summary>Terse helpers for constructing cards by rank/suit in tests.</summary>
    internal static class Cards
    {
        /// <summary>Build the wire byte for a standard rank+suit pair: (rank-1)*4 + suit.</summary>
        public static Card Of(Rank rank, Suit suit) => new Card((byte)(((int)rank - 1) * 4 + (int)suit));

        public static Card Ace(Suit suit) => Of(Rank.Ace, suit);
    }
}
