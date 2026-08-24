using System.Collections.Generic;
using LeastCount.Core;
using Xunit;

namespace LeastCount.Core.Tests
{
    public class ScoringTests
    {
        [Fact]
        public void FaceValues_AceThroughKing()
        {
            Assert.Equal(1, Scoring.PointValue(Cards.Ace(Suit.Spades), Rank.None));
            Assert.Equal(7, Scoring.PointValue(Cards.Of(Rank.Seven, Suit.Clubs), Rank.None));
            Assert.Equal(10, Scoring.PointValue(Cards.Of(Rank.Ten, Suit.Hearts), Rank.None));
            Assert.Equal(11, Scoring.PointValue(Cards.Of(Rank.Jack, Suit.Diamonds), Rank.None));
            Assert.Equal(12, Scoring.PointValue(Cards.Of(Rank.Queen, Suit.Spades), Rank.None));
            Assert.Equal(13, Scoring.PointValue(Cards.Of(Rank.King, Suit.Hearts), Rank.None));
        }

        [Fact]
        public void CutJokerRank_ScoresZero_InAllSuits()
        {
            // Jacks are the round's joker -> every Jack is worth 0.
            foreach (Suit s in new[] { Suit.Spades, Suit.Clubs, Suit.Diamonds, Suit.Hearts })
                Assert.Equal(0, Scoring.PointValue(Cards.Of(Rank.Jack, s), Rank.Jack));
        }

        [Fact]
        public void PrintedJoker_AlwaysScoresZero()
        {
            Assert.Equal(0, Scoring.PointValue(new Card(52), Rank.None));
            Assert.Equal(0, Scoring.PointValue(new Card(53), Rank.Seven));
        }

        [Fact]
        public void KingOfSpades_And_KingOfHearts_ScoreTheSame()
        {
            // Regression: legacy code summed raw bytes so K♠ (48) and K♥ (51) scored differently.
            Assert.Equal(
                Scoring.PointValue(Cards.Of(Rank.King, Suit.Spades), Rank.None),
                Scoring.PointValue(Cards.Of(Rank.King, Suit.Hearts), Rank.None));
        }

        [Fact]
        public void HandValue_SumsPointValues_WithJokerRankZeroed()
        {
            var hand = new List<Card>
            {
                Cards.Of(Rank.King, Suit.Spades),  // 13
                Cards.Of(Rank.Seven, Suit.Hearts), // 7 -> joker, 0
                Cards.Ace(Suit.Clubs),             // 1
                new Card(52),                      // printed joker, 0
            };
            Assert.Equal(14, Scoring.HandValue(hand, Rank.Seven));
        }
    }
}
