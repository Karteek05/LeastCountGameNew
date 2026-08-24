using System.Collections.Generic;
using LeastCount.Core;
using Xunit;

namespace LeastCount.Core.Tests
{
    public class MatchTests
    {
        private static RoundScore Pts(int seat, int points) => new RoundScore(seat, points, points, false, false);

        [Fact]
        public void LowestTotal_Wins_NotHighest()
        {
            // Regression for the inverted-winner bug.
            var totals = new Dictionary<int, int> { { 0, 40 }, { 1, 12 }, { 2, 88 } };
            Assert.Equal(1, Match.LowestSeat(totals));
        }

        [Fact]
        public void CrossingThreshold_Eliminates()
        {
            var m = new Match(new[] { 0, 1 });
            m.ApplyRound(new[] { Pts(0, 150), Pts(1, 40) });
            m.ApplyRound(new[] { Pts(0, 60), Pts(1, 10) }); // seat 0 -> 210 > 200
            Assert.True(m.IsEliminated(0));
            Assert.False(m.IsEliminated(1));
        }

        [Fact]
        public void ExactlyThreshold_IsNotEliminated()
        {
            var m = new Match(new[] { 0, 1 });
            m.ApplyRound(new[] { Pts(0, 200), Pts(1, 5) });
            Assert.False(m.IsEliminated(0)); // strictly greater than 200 eliminates
        }

        [Fact]
        public void MatchOver_WhenOnePlayerRemains_LastSurvivorWins()
        {
            var m = new Match(new[] { 0, 1, 2 });
            m.ApplyRound(new[] { Pts(0, 201), Pts(1, 201), Pts(2, 10) });
            Assert.True(m.IsOver);
            Assert.Equal(2, m.Winner);
        }

        [Fact]
        public void MatchNotOver_WhileTwoRemain()
        {
            var m = new Match(new[] { 0, 1, 2 });
            m.ApplyRound(new[] { Pts(0, 201), Pts(1, 50), Pts(2, 10) });
            Assert.False(m.IsOver);
            Assert.Null(m.Winner);
        }
    }
}
