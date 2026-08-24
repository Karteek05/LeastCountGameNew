using System.Collections.Generic;
using System.Linq;
using LeastCount.Core;
using Xunit;

namespace LeastCount.Core.Tests
{
    public class DeclareScoringTests
    {
        private static IReadOnlyList<Card> Hand(params Card[] cs) => cs;

        private static IReadOnlyDictionary<int, IReadOnlyList<Card>> Hands(
            params (int seat, IReadOnlyList<Card> cards)[] hands)
            => hands.ToDictionary(h => h.seat, h => h.cards);

        [Fact]
        public void DeclarerLowest_ScoresZero_OthersScoreHandValue()
        {
            var hands = Hands(
                (0, Hand(Cards.Of(Rank.Two, Suit.Spades))),              // declarer, value 2
                (1, Hand(Cards.Of(Rank.King, Suit.Hearts))),            // 13
                (2, Hand(Cards.Of(Rank.Five, Suit.Clubs))));           // 5

            var scores = DeclareScoring.Score(hands, declarerSeat: 0, Rank.None)
                                       .ToDictionary(s => s.Seat);

            Assert.Equal(0, scores[0].Points);
            Assert.Equal(13, scores[1].Points);
            Assert.Equal(5, scores[2].Points);
        }

        [Fact]
        public void DeclarerTiesForLowest_BothZero()
        {
            var hands = Hands(
                (0, Hand(Cards.Of(Rank.Five, Suit.Spades))),  // declarer, 5
                (1, Hand(Cards.Of(Rank.Five, Suit.Hearts))),  // 5 (tie)
                (2, Hand(Cards.Of(Rank.King, Suit.Clubs))));  // 13

            var scores = DeclareScoring.Score(hands, 0, Rank.None).ToDictionary(s => s.Seat);

            Assert.Equal(0, scores[0].Points); // declarer
            Assert.Equal(0, scores[1].Points); // tied-lowest
            Assert.Equal(13, scores[2].Points);
        }

        [Fact]
        public void DeclarerCaught_Pays20PlusOwnHand_LowestZeroed()
        {
            var hands = Hands(
                (0, Hand(Cards.Of(Rank.Ten, Suit.Spades))),   // declarer, 10
                (1, Hand(Cards.Of(Rank.Three, Suit.Hearts))), // 3 (strictly lower)
                (2, Hand(Cards.Of(Rank.King, Suit.Clubs))));  // 13

            var scores = DeclareScoring.Score(hands, 0, Rank.None).ToDictionary(s => s.Seat);

            Assert.Equal(30, scores[0].Points); // 20 + 10
            Assert.Equal(0, scores[1].Points);  // lowest
            Assert.Equal(13, scores[2].Points);
        }

        [Fact]
        public void JokerRank_ZeroesCardsBeforeComparing()
        {
            // Sevens are joker. Declarer holds two 7s (=0), so declarer is lowest and scores 0.
            var hands = Hands(
                (0, Hand(Cards.Of(Rank.Seven, Suit.Spades), Cards.Of(Rank.Seven, Suit.Hearts))),
                (1, Hand(Cards.Of(Rank.Two, Suit.Clubs))));

            var scores = DeclareScoring.Score(hands, 0, Rank.Seven).ToDictionary(s => s.Seat);
            Assert.Equal(0, scores[0].HandValue);
            Assert.Equal(0, scores[0].Points);
            Assert.Equal(2, scores[1].Points);
        }
    }
}
