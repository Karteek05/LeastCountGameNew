using System.Collections.Generic;
using System.Linq;

namespace LeastCount.Core
{
    public enum MeldKind
    {
        /// <summary>The played cards do not form any legal shape.</summary>
        Invalid = 0,
        Single = 1,
        Set = 2,
        Sequence = 3,
    }

    /// <summary>
    /// Classifies a group of played cards into a legal Least Count shape and answers
    /// whether it may be played on a given top card.
    ///
    /// Open rules questions are resolved here with explicit, flagged defaults so they are
    /// easy to flip once confirmed (see CLAUDE.md "Unresolved"):
    ///  - A cut-joker-rank card does NOT act as a sequence wildcard (default: no).
    ///  - A cut-joker-rank card matches only its printed suit/rank when played (default: no wildcard match).
    /// These joker questions only affect play legality, never scoring; scoring lives in <see cref="Scoring"/>.
    /// </summary>
    public static class Melds
    {
        /// <summary>Two standard cards match if they share a suit or a rank. Printed jokers match nothing.</summary>
        public static bool Matches(Card a, Card b)
        {
            if (a.IsPrintedJoker || b.IsPrintedJoker) return false;
            return a.Suit == b.Suit || a.Rank == b.Rank;
        }

        /// <summary>Classify played cards into a shape, ignoring any match-the-top requirement.</summary>
        public static MeldKind Classify(IReadOnlyList<Card> cards)
        {
            if (cards == null || cards.Count == 0) return MeldKind.Invalid;
            if (cards.Any(c => c.IsPrintedJoker))
                // A printed joker has no rank/suit, so it can't combine into a set or sequence.
                // It can only ever be a lone single.
                return cards.Count == 1 ? MeldKind.Single : MeldKind.Invalid;

            if (cards.Count == 1) return MeldKind.Single;
            if (IsSet(cards)) return MeldKind.Set;
            if (IsSequence(cards)) return MeldKind.Sequence;
            return MeldKind.Invalid;
        }

        /// <summary>Set = 2+ cards of the same rank.</summary>
        public static bool IsSet(IReadOnlyList<Card> cards)
        {
            if (cards.Count < 2) return false;
            Rank r = cards[0].Rank;
            return cards.All(c => !c.IsPrintedJoker && c.Rank == r);
        }

        /// <summary>Sequence = 3+ consecutive ranks in a single suit. Ace runs low (A-2-3) or high (Q-K-A).</summary>
        public static bool IsSequence(IReadOnlyList<Card> cards)
        {
            if (cards.Count < 3) return false;
            Suit s = cards[0].Suit;
            if (cards.Any(c => c.IsPrintedJoker || c.Suit != s)) return false;

            var ranks = cards.Select(c => (int)c.Rank).ToList();
            if (ranks.Distinct().Count() != ranks.Count) return false; // no duplicate ranks

            if (IsConsecutive(ranks)) return true;

            // Ace-high: a suit holds at most one Ace, so promoting 1 -> 14 is unambiguous.
            if (ranks.Contains((int)Rank.Ace))
            {
                var high = ranks.Select(r => r == (int)Rank.Ace ? 14 : r).ToList();
                if (IsConsecutive(high)) return true;
            }
            return false;
        }

        private static bool IsConsecutive(List<int> ranks)
        {
            ranks.Sort();
            return ranks[ranks.Count - 1] - ranks[0] == ranks.Count - 1;
        }

        /// <summary>True if at least one played card matches the top card by suit or rank.</summary>
        public static bool MatchesTop(IReadOnlyList<Card> cards, Card top)
            => cards.Any(c => Matches(c, top));

        /// <summary>
        /// Full legality check for a no-draw play. When <paramref name="matchingRequired"/> is false
        /// (i.e. the player drew this turn), the match-the-top rule is switched off.
        /// Does NOT check hand ownership or the retain-one rule — that is the round engine's job.
        /// </summary>
        public static bool IsLegalPlay(IReadOnlyList<Card> cards, Card top, bool matchingRequired)
        {
            if (Classify(cards) == MeldKind.Invalid) return false;
            if (!matchingRequired) return true;
            return MatchesTop(cards, top);
        }
    }
}
