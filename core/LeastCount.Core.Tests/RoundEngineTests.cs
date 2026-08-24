using System.Collections.Generic;
using System.Linq;
using LeastCount.Core;
using Xunit;

namespace LeastCount.Core.Tests
{
    public class RoundEngineTests
    {
        // Build a deterministic engine from explicit hands so tests aren't at the mercy of the shuffle.
        private static RoundEngine Engine(
            Card opening,
            Rank jokerRank,
            IReadOnlyList<Card>[] hands,
            IReadOnlyList<Card>? pool = null)
        {
            // A cut joker whose rank == jokerRank (suit irrelevant to the round).
            Card cut = Cards.Of(jokerRank == Rank.None ? Rank.King : jokerRank, Suit.Spades);
            var deal = new RoundDeal(hands, cut, opening, pool ?? new List<Card>(), new List<Card>());
            var seats = Enumerable.Range(0, hands.Length).ToList();
            return new RoundEngine(seats, deal, new SeededRng(1));
        }

        [Fact]
        public void Play_MatchingSingle_MovesToNextSeatAndSetsTop()
        {
            var e = Engine(
                opening: Cards.Of(Rank.Ten, Suit.Diamonds),
                jokerRank: Rank.None,
                hands: new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Four, Suit.Diamonds), Cards.Of(Rank.King, Suit.Clubs) }, // seat 0
                    new[] { Cards.Of(Rank.Nine, Suit.Hearts), Cards.Of(Rank.Two, Suit.Spades) },   // seat 1
                });

            var r = e.Apply(0, Move.Play(Cards.Of(Rank.Four, Suit.Diamonds)));
            Assert.True(r.Ok, r.Error);
            Assert.Equal(1, e.CurrentSeat);
            Assert.Equal(Cards.Of(Rank.Four, Suit.Diamonds), e.TopOfPile);
        }

        [Fact]
        public void Play_NonMatchingSingle_Rejected()
        {
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Four, Suit.Clubs), Cards.Of(Rank.King, Suit.Clubs) },
                    new[] { Cards.Of(Rank.Nine, Suit.Hearts), Cards.Of(Rank.Two, Suit.Spades) },
                });

            var r = e.Apply(0, Move.Play(Cards.Of(Rank.Four, Suit.Clubs)));
            Assert.False(r.Ok);
            Assert.Equal(0, e.CurrentSeat); // turn did not advance
        }

        [Fact]
        public void Play_EmptyingHand_Rejected_RetainOne()
        {
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Four, Suit.Diamonds) }, // only one card
                    new[] { Cards.Of(Rank.Nine, Suit.Hearts), Cards.Of(Rank.Two, Suit.Spades) },
                });

            var r = e.Apply(0, Move.Play(Cards.Of(Rank.Four, Suit.Diamonds)));
            Assert.False(r.Ok);
            Assert.Contains("at least one card", r.Error);
        }

        [Fact]
        public void DrawPlay_SwitchesMatchingOff_AndDrawnCardCannotBePlayed()
        {
            // Top 10♦. Hand can't match, but drawing lets it play a non-matching single.
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Four, Suit.Clubs), Cards.Of(Rank.King, Suit.Clubs) },
                    new[] { Cards.Of(Rank.Nine, Suit.Hearts), Cards.Of(Rank.Two, Suit.Spades) },
                },
                pool: new[] { Cards.Of(Rank.Six, Suit.Hearts) }); // top of deck

            // Playing the just-drawn 6♥ is illegal.
            var bad = e.Apply(0, Move.DrawPlay(DrawSource.Deck, Cards.Of(Rank.Six, Suit.Hearts)));
            Assert.False(bad.Ok);

            // Playing an existing card after the draw is fine (no match needed).
            var ok = e.Apply(0, Move.DrawPlay(DrawSource.Deck, Cards.Of(Rank.Four, Suit.Clubs)));
            Assert.True(ok.Ok, ok.Error);
            Assert.Equal(2, e.HandOf(0).Count); // 2 + 1 drawn - 1 played
            Assert.Contains(Cards.Of(Rank.Six, Suit.Hearts), e.HandOf(0));
        }

        [Fact]
        public void MultiCardPlay_LeavesChosenCardOnTop()
        {
            // Sequence 7♦8♦9♦ on top of 10♦ (9♦ matches by suit); choose 7♦ to remain on top.
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[]
                    {
                        Cards.Of(Rank.Seven, Suit.Diamonds), Cards.Of(Rank.Eight, Suit.Diamonds),
                        Cards.Of(Rank.Nine, Suit.Diamonds), Cards.Of(Rank.King, Suit.Clubs),
                    },
                    new[] { Cards.Of(Rank.Two, Suit.Spades), Cards.Of(Rank.Three, Suit.Spades) },
                });

            var seq = new[] { Cards.Of(Rank.Seven, Suit.Diamonds), Cards.Of(Rank.Eight, Suit.Diamonds), Cards.Of(Rank.Nine, Suit.Diamonds) };
            var r = e.Apply(0, Move.Play(seq, topChoice: Cards.Of(Rank.Seven, Suit.Diamonds)));
            Assert.True(r.Ok, r.Error);
            Assert.Equal(Cards.Of(Rank.Seven, Suit.Diamonds), e.TopOfPile);
            Assert.Single(e.HandOf(0)); // K♣ retained
        }

        [Fact]
        public void Declare_BeforeFullCircle_Rejected_ThenAllowedAfter()
        {
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Four, Suit.Diamonds), Cards.Of(Rank.King, Suit.Clubs) },
                    new[] { Cards.Of(Rank.Nine, Suit.Diamonds), Cards.Of(Rank.Two, Suit.Spades) },
                });

            Assert.False(e.Apply(0, Move.Declare()).Ok); // no full circle yet

            Assert.True(e.Apply(0, Move.Play(Cards.Of(Rank.Four, Suit.Diamonds))).Ok);
            Assert.True(e.Apply(1, Move.Play(Cards.Of(Rank.Nine, Suit.Diamonds))).Ok);

            // Back to seat 0; a full circle has completed.
            var d = e.Apply(0, Move.Declare());
            Assert.True(d.Ok, d.Error);
            Assert.True(e.Finished);
            Assert.True(d.RoundEnded);
            Assert.Equal(0, e.DeclarerSeat);
        }

        [Fact]
        public void WrongSeat_Rejected()
        {
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Four, Suit.Diamonds), Cards.Of(Rank.King, Suit.Clubs) },
                    new[] { Cards.Of(Rank.Nine, Suit.Hearts), Cards.Of(Rank.Two, Suit.Spades) },
                });

            Assert.False(e.Apply(1, Move.Play(Cards.Of(Rank.Nine, Suit.Hearts))).Ok);
        }

        [Fact]
        public void DrawFromPile_TakesTopCard_ThenPlays()
        {
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Four, Suit.Clubs), Cards.Of(Rank.King, Suit.Clubs) },
                    new[] { Cards.Of(Rank.Nine, Suit.Hearts), Cards.Of(Rank.Two, Suit.Spades) },
                });

            var r = e.Apply(0, Move.DrawPlay(DrawSource.Pile, Cards.Of(Rank.King, Suit.Clubs)));
            Assert.True(r.Ok, r.Error);
            // Took 10♦ into hand, played K♣ on top.
            Assert.Contains(Cards.Of(Rank.Ten, Suit.Diamonds), e.HandOf(0));
            Assert.Equal(Cards.Of(Rank.King, Suit.Clubs), e.TopOfPile);
        }

        [Fact]
        public void EmptyDrawPile_ReshufflesDiscardKeepingTop()
        {
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Ten, Suit.Clubs), Cards.Of(Rank.Five, Suit.Hearts),
                            Cards.Of(Rank.Six, Suit.Hearts), Cards.Of(Rank.Seven, Suit.Hearts) },
                    new[] { Cards.Of(Rank.Ten, Suit.Spades), Cards.Of(Rank.Two, Suit.Spades),
                            Cards.Of(Rank.Three, Suit.Spades), Cards.Of(Rank.Four, Suit.Spades) },
                },
                pool: new List<Card>()); // draw pile starts empty

            // Grow the discard: 10♣ on 10♦ (rank match), then 10♠ on 10♣ (rank match).
            Assert.True(e.Apply(0, Move.Play(Cards.Of(Rank.Ten, Suit.Clubs))).Ok);
            Assert.True(e.Apply(1, Move.Play(Cards.Of(Rank.Ten, Suit.Spades))).Ok);

            // Seat 0 again, first circle done, draw pile empty (discard = 10♦,10♣,10♠).
            Assert.Equal(0, e.DrawPileCount);
            var r = e.Apply(0, Move.DrawPlay(DrawSource.Deck, Cards.Of(Rank.Five, Suit.Hearts)));
            Assert.True(r.Ok, r.Error);

            // Discard's two lower cards (10♦,10♣) were reshuffled into the draw pile, keeping top 10♠;
            // one was then drawn, leaving one in the pile. 5♥ is now on top.
            Assert.Equal(1, e.DrawPileCount);
            Assert.Equal(Cards.Of(Rank.Five, Suit.Hearts), e.TopOfPile);
        }

        [Fact]
        public void ViewFor_NeverExposesOtherHands()
        {
            var e = Engine(
                Cards.Of(Rank.Ten, Suit.Diamonds), Rank.None,
                new IReadOnlyList<Card>[]
                {
                    new[] { Cards.Of(Rank.Four, Suit.Diamonds), Cards.Of(Rank.King, Suit.Clubs) },
                    new[] { Cards.Of(Rank.Nine, Suit.Hearts), Cards.Of(Rank.Two, Suit.Spades) },
                });

            var view = e.ViewFor(0);
            Assert.Equal(2, view.Hand.Count);
            Assert.Equal(2, view.HandCounts[1]);       // only the count of seat 1
            // The view type has no field carrying another seat's cards; counts are all a client gets.
            Assert.All(view.Hand, c => Assert.Contains(c, e.HandOf(0)));
        }

        [Fact]
        public void LegalMoves_AllApplyCleanly()
        {
            var deal = Deck.Deal(4, 7, new SeededRng(42));
            var seats = new[] { 0, 1, 2, 3 };

            // Play a few random legal moves and assert each returned move is genuinely applicable.
            var e = new RoundEngine(seats, deal, new SeededRng(42));
            for (int step = 0; step < 20 && !e.Finished; step++)
            {
                var moves = e.LegalMoves().ToList();
                Assert.NotEmpty(moves);
                // Prefer a non-declare move so the round keeps going; every generated move must apply.
                var move = moves.FirstOrDefault(m => m.Type != MoveType.Declare) ?? moves[0];
                var res = e.Apply(e.CurrentSeat, move);
                Assert.True(res.Ok, res.Error);
            }
        }
    }
}
