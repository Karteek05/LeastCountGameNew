using LeastCount.Core;
using Xunit;

namespace LeastCount.Core.Tests
{
    public class CardTests
    {
        [Theory]
        [InlineData(0, Rank.Ace, Suit.Spades)]
        [InlineData(1, Rank.Ace, Suit.Clubs)]
        [InlineData(2, Rank.Ace, Suit.Diamonds)]
        [InlineData(3, Rank.Ace, Suit.Hearts)]
        [InlineData(4, Rank.Two, Suit.Spades)]
        [InlineData(48, Rank.King, Suit.Spades)]
        [InlineData(51, Rank.King, Suit.Hearts)]
        public void StandardCards_DecodeRankAndSuit(byte v, Rank rank, Suit suit)
        {
            var c = new Card(v);
            Assert.False(c.IsPrintedJoker);
            Assert.Equal(rank, c.Rank);
            Assert.Equal(suit, c.Suit);
        }

        [Theory]
        [InlineData(52)]
        [InlineData(53)]
        public void PrintedJokers_HaveNoRankOrSuit(byte v)
        {
            var c = new Card(v);
            Assert.True(c.IsPrintedJoker);
            Assert.Equal(Rank.Joker, c.Rank);
            Assert.Equal(Suit.None, c.Suit);
        }

        [Fact]
        public void JokerRangeCheck_HappensBeforeArithmetic()
        {
            // 52/4+1 == 14 would be a garbage rank if the range check were skipped.
            Assert.Equal(Rank.Joker, Card.GetRank(52));
            Assert.Equal(Suit.None, Card.GetSuit(53));
        }

        [Fact]
        public void DeckSize_Is54()
        {
            Assert.Equal(54, Card.DeckSize);
            Assert.Equal(52, Card.FirstPrintedJoker);
        }

        [Fact]
        public void PoolIsEmptySentinel_IsNotAPrintedJoker()
        {
            var c = new Card(Card.PoolIsEmpty);
            Assert.False(c.IsPrintedJoker);
        }
    }
}
