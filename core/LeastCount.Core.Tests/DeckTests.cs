using System.Collections.Generic;
using System.Linq;
using LeastCount.Core;
using Xunit;

namespace LeastCount.Core.Tests
{
    public class DeckTests
    {
        [Fact]
        public void FullDeck_Has54DistinctCards()
        {
            var deck = Deck.BuildFullDeck();
            Assert.Equal(54, deck.Count);
            Assert.Equal(54, deck.Select(c => c.Value).Distinct().Count());
        }

        [Fact]
        public void Deal_GivesEachPlayerSevenCards()
        {
            var deal = Deck.Deal(playerCount: 4, cardsPerPlayer: 7, new SeededRng(123));
            Assert.Equal(4, deal.Hands.Count);
            Assert.All(deal.Hands, h => Assert.Equal(7, h.Count));
        }

        [Fact]
        public void Deal_ConservesEveryCard()
        {
            var deal = Deck.Deal(4, 7, new SeededRng(7));
            var all = new List<Card>();
            foreach (var h in deal.Hands) all.AddRange(h);
            all.Add(deal.CutJoker);
            all.Add(deal.Opening);
            all.AddRange(deal.Pool);
            all.AddRange(deal.Burned);
            Assert.Equal(54, all.Count);
            Assert.Equal(54, all.Select(c => c.Value).Distinct().Count());
        }

        [Fact]
        public void CutJoker_And_Opening_AreNeverPrintedJokers()
        {
            // Sweep many seeds so the re-draw path is actually exercised.
            for (int seed = 0; seed < 200; seed++)
            {
                var deal = Deck.Deal(6, 7, new SeededRng(seed));
                Assert.False(deal.CutJoker.IsPrintedJoker);
                Assert.False(deal.Opening.IsPrintedJoker);
                Assert.NotEqual(Rank.Joker, deal.JokerRank);
                Assert.All(deal.Burned, c => Assert.True(c.IsPrintedJoker));
            }
        }

        [Fact]
        public void Deal_IsDeterministicForAGivenSeed()
        {
            var a = Deck.Deal(4, 7, new SeededRng(999));
            var b = Deck.Deal(4, 7, new SeededRng(999));
            Assert.Equal(a.CutJoker.Value, b.CutJoker.Value);
            Assert.Equal(a.Opening.Value, b.Opening.Value);
            Assert.Equal(
                a.Hands.SelectMany(h => h.Select(c => c.Value)),
                b.Hands.SelectMany(h => h.Select(c => c.Value)));
        }

        [Fact]
        public void SixPlayers_SevenCards_LeavesTightPool()
        {
            // 6*7 = 42 dealt, + cut joker + opening => ~10 left (minus any burned jokers).
            var deal = Deck.Deal(6, 7, new SeededRng(1));
            Assert.InRange(deal.Pool.Count, 8, 10);
        }
    }
}
