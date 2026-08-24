using System.Collections.Generic;
using System.Linq;

namespace LeastCount.Core
{
    /// <summary>
    /// Tracks cumulative scores across rounds. A player crossing the elimination threshold is out;
    /// play continues until one player remains, who wins.
    ///
    /// Fixes the legacy bug where the winner was the HIGHEST total. In Least Count the LOWEST hand
    /// wins a round and the LAST surviving player wins the match.
    /// </summary>
    public sealed class Match
    {
        /// <summary>A player is eliminated once their total strictly exceeds this.</summary>
        public const int EliminationThreshold = 200;

        private readonly Dictionary<int, int> _totals;
        private readonly HashSet<int> _eliminated = new HashSet<int>();

        public Match(IEnumerable<int> seats)
        {
            _totals = seats.ToDictionary(s => s, _ => 0);
        }

        public IReadOnlyDictionary<int, int> Totals => _totals;

        public bool IsEliminated(int seat) => _eliminated.Contains(seat);

        /// <summary>Seats still in the match.</summary>
        public IReadOnlyList<int> ActiveSeats => _totals.Keys.Where(s => !_eliminated.Contains(s)).OrderBy(s => s).ToList();

        /// <summary>Apply one round's scores; any player crossing the threshold is eliminated.</summary>
        public void ApplyRound(IEnumerable<RoundScore> scores)
        {
            foreach (var s in scores)
            {
                if (!_totals.ContainsKey(s.Seat)) continue;
                _totals[s.Seat] += s.Points;
                if (_totals[s.Seat] > EliminationThreshold)
                    _eliminated.Add(s.Seat);
            }
        }

        /// <summary>True once at most one player remains.</summary>
        public bool IsOver => ActiveSeats.Count <= 1;

        /// <summary>The winning seat if the match is over, otherwise null.</summary>
        public int? Winner => IsOver ? ActiveSeats.FirstOrDefault() : (int?)null;

        /// <summary>
        /// Seat with the lowest running total among a set (round winner by lowest hand).
        /// Ties are broken by lowest seat index for determinism; callers wanting shared wins
        /// should inspect totals directly.
        /// </summary>
        public static int LowestSeat(IReadOnlyDictionary<int, int> handValues)
            => handValues.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
    }
}
