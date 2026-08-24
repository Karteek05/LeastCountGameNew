using System.Collections.Generic;
using LeastCount.Core;
using Xunit;

namespace LeastCount.Core.Tests
{
    public class MeldTests
    {
        private static List<Card> L(params Card[] cs) => new List<Card>(cs);

        [Fact]
        public void Single_IsSingle()
        {
            Assert.Equal(MeldKind.Single, Melds.Classify(L(Cards.Of(Rank.Ten, Suit.Diamonds))));
        }

        [Fact]
        public void SameRank_IsSet()
        {
            Assert.Equal(MeldKind.Set, Melds.Classify(L(
                Cards.Of(Rank.Ten, Suit.Clubs), Cards.Of(Rank.Ten, Suit.Hearts))));
        }

        [Fact]
        public void ConsecutiveSameSuit_IsSequence()
        {
            Assert.Equal(MeldKind.Sequence, Melds.Classify(L(
                Cards.Of(Rank.Seven, Suit.Diamonds),
                Cards.Of(Rank.Eight, Suit.Diamonds),
                Cards.Of(Rank.Nine, Suit.Diamonds))));
        }

        [Fact]
        public void Sequence_AceLow_A23_IsValid()
        {
            Assert.True(Melds.IsSequence(L(
                Cards.Ace(Suit.Spades),
                Cards.Of(Rank.Two, Suit.Spades),
                Cards.Of(Rank.Three, Suit.Spades))));
        }

        [Fact]
        public void Sequence_AceHigh_QKA_IsValid()
        {
            Assert.True(Melds.IsSequence(L(
                Cards.Of(Rank.Queen, Suit.Hearts),
                Cards.Of(Rank.King, Suit.Hearts),
                Cards.Ace(Suit.Hearts))));
        }

        [Fact]
        public void Sequence_WrapAround_KA2_IsInvalid()
        {
            // Ace is either low or high, never a pivot: K-A-2 is not a run.
            Assert.False(Melds.IsSequence(L(
                Cards.Of(Rank.King, Suit.Clubs),
                Cards.Ace(Suit.Clubs),
                Cards.Of(Rank.Two, Suit.Clubs))));
        }

        [Fact]
        public void Sequence_MixedSuits_IsInvalid()
        {
            Assert.False(Melds.IsSequence(L(
                Cards.Of(Rank.Seven, Suit.Diamonds),
                Cards.Of(Rank.Eight, Suit.Clubs),
                Cards.Of(Rank.Nine, Suit.Diamonds))));
        }

        [Fact]
        public void TwoCardSequence_IsInvalid()
        {
            Assert.NotEqual(MeldKind.Sequence, Melds.Classify(L(
                Cards.Of(Rank.Seven, Suit.Diamonds),
                Cards.Of(Rank.Eight, Suit.Diamonds))));
        }

        [Fact]
        public void PrintedJoker_OnlyValidAsLoneSingle()
        {
            Assert.Equal(MeldKind.Single, Melds.Classify(L(new Card(52))));
            Assert.Equal(MeldKind.Invalid, Melds.Classify(L(new Card(52), Cards.Of(Rank.Ten, Suit.Clubs))));
        }

        [Fact]
        public void Matches_BySuitOrRank()
        {
            var top = Cards.Of(Rank.Ten, Suit.Diamonds);
            Assert.True(Melds.Matches(Cards.Of(Rank.Four, Suit.Diamonds), top)); // suit
            Assert.True(Melds.Matches(Cards.Of(Rank.Ten, Suit.Clubs), top));     // rank
            Assert.False(Melds.Matches(Cards.Of(Rank.Four, Suit.Clubs), top));   // neither
        }

        [Fact]
        public void NothingMatches_PrintedJokerOnTop()
        {
            var top = new Card(52);
            Assert.False(Melds.Matches(Cards.Of(Rank.Ten, Suit.Clubs), top));
        }

        [Fact]
        public void NoDrawPlay_RequiresMatch()
        {
            var top = Cards.Of(Rank.Ten, Suit.Diamonds);
            var play = L(Cards.Of(Rank.Four, Suit.Clubs)); // matches neither
            Assert.False(Melds.IsLegalPlay(play, top, matchingRequired: true));
        }

        [Fact]
        public void DrawPlay_SwitchesMatchingOff()
        {
            var top = Cards.Of(Rank.Ten, Suit.Diamonds);
            var play = L(Cards.Of(Rank.Four, Suit.Clubs)); // matches neither, but a draw was paid
            Assert.True(Melds.IsLegalPlay(play, top, matchingRequired: false));
        }

        [Fact]
        public void Set_LegalWhenOneCardMatchesTopBySuit()
        {
            // Top 10♦. Set of Jacks led by J♦ matches by suit.
            var top = Cards.Of(Rank.Ten, Suit.Diamonds);
            var set = L(Cards.Of(Rank.Jack, Suit.Diamonds),
                        Cards.Of(Rank.Jack, Suit.Hearts),
                        Cards.Of(Rank.Jack, Suit.Spades));
            Assert.Equal(MeldKind.Set, Melds.Classify(set));
            Assert.True(Melds.IsLegalPlay(set, top, matchingRequired: true));
        }
    }
}
