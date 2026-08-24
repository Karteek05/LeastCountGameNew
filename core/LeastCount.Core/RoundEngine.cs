using System;
using System.Collections.Generic;
using System.Linq;

namespace LeastCount.Core
{
    /// <summary>Outcome of attempting a move.</summary>
    public readonly struct MoveResult
    {
        public bool Ok { get; }
        public string? Error { get; }
        /// <summary>True if this move ended the round (a declare).</summary>
        public bool RoundEnded { get; }

        private MoveResult(bool ok, string? error, bool roundEnded)
        {
            Ok = ok; Error = error; RoundEnded = roundEnded;
        }

        public static MoveResult Success(bool roundEnded = false) => new MoveResult(true, null, roundEnded);
        public static MoveResult Fail(string error) => new MoveResult(false, error, false);
    }

    /// <summary>
    /// The authoritative, server-side turn engine for one round. Owns the full truth (all hands, the
    /// draw pile, the discard pile) and validates every move. Clients only ever receive a
    /// <see cref="ViewFor"/> projection, never the whole state.
    ///
    /// This replaces the legacy Unity turn state machine (which assumed one card per turn). A turn is
    /// exactly one of: Play (no draw, must match), Draw+Play (match rule switched off), or Declare.
    /// </summary>
    public sealed class RoundEngine
    {
        private readonly IReadOnlyList<int> _seats;
        private readonly Dictionary<int, List<Card>> _hands;
        private readonly List<Card> _drawPile;   // top = last element
        private readonly List<Card> _discard;    // top = last element
        private readonly IRng _rng;

        private int _turnIndex;
        private int _turnsTaken;

        public Rank JokerRank { get; }
        public Card CutJoker { get; }
        public bool Finished { get; private set; }
        public int? DeclarerSeat { get; private set; }

        public RoundEngine(IReadOnlyList<int> seats, RoundDeal deal, IRng rng)
        {
            if (seats == null || seats.Count < 2) throw new ArgumentException("Need at least 2 seats.", nameof(seats));
            if (deal.Hands.Count != seats.Count) throw new ArgumentException("Deal hand count must match seat count.");

            _seats = seats.ToList();
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _hands = new Dictionary<int, List<Card>>();
            for (int i = 0; i < _seats.Count; i++)
                _hands[_seats[i]] = deal.Hands[i].ToList();

            _drawPile = deal.Pool.ToList();
            _discard = new List<Card> { deal.Opening };
            CutJoker = deal.CutJoker;
            JokerRank = deal.JokerRank;
            _turnIndex = 0;
            _turnsTaken = 0;
        }

        public int CurrentSeat => _seats[_turnIndex];

        /// <summary>Top card of the discard pile — what a no-draw play must match.</summary>
        public Card TopOfPile => _discard[_discard.Count - 1];

        public int DrawPileCount => _drawPile.Count;

        /// <summary>A player may declare only on their turn and only after one full circle has completed.</summary>
        public bool CanDeclare(int seat) => !Finished && seat == CurrentSeat && _turnsTaken >= _seats.Count;

        /// <summary>Read-only snapshot of a seat's hand (server-side use / tests). Not for sending to other clients.</summary>
        public IReadOnlyList<Card> HandOf(int seat) => _hands[seat];

        private IReadOnlyDictionary<int, int> HandCounts()
            => _seats.ToDictionary(s => s, s => _hands[s].Count);

        /// <summary>The private, leak-free view a given seat is allowed to receive.</summary>
        public PlayerView ViewFor(int seat) => new PlayerView(
            seat,
            _hands[seat].OrderBy(c => c.Value).ToList(),
            TopOfPile,
            JokerRank,
            _drawPile.Count,
            HandCounts(),
            CurrentSeat,
            CanDeclare(seat));

        /// <summary>Current hand values for every seat, joker-aware. Used at declare time.</summary>
        public IReadOnlyDictionary<int, IReadOnlyList<Card>> AllHands()
            => _seats.ToDictionary(s => s, s => (IReadOnlyList<Card>)_hands[s]);

        /// <summary>Validate and apply a move by <paramref name="seat"/>.</summary>
        public MoveResult Apply(int seat, Move move)
        {
            if (Finished) return MoveResult.Fail("Round is already finished.");
            if (seat != CurrentSeat) return MoveResult.Fail($"Not seat {seat}'s turn (current: {CurrentSeat}).");

            switch (move.Type)
            {
                case MoveType.Declare: return ApplyDeclare(seat);
                case MoveType.Play: return ApplyPlay(seat, move, drew: false);
                case MoveType.DrawPlay: return ApplyPlay(seat, move, drew: true);
                default: return MoveResult.Fail("Unknown move type.");
            }
        }

        private MoveResult ApplyDeclare(int seat)
        {
            if (!CanDeclare(seat))
                return MoveResult.Fail("Cannot declare before one full circle has completed.");
            Finished = true;
            DeclarerSeat = seat;
            return MoveResult.Success(roundEnded: true);
        }

        private MoveResult ApplyPlay(int seat, Move move, bool drew)
        {
            var hand = _hands[seat];

            // Work on a copy of the hand so a rejected move never mutates state.
            var working = new List<Card>(hand);
            Card drawn = default;

            if (drew)
            {
                if (move.Source is not DrawSource src)
                    return MoveResult.Fail("Draw source required.");

                if (src == DrawSource.Deck)
                {
                    if (!EnsureDrawable())
                        return MoveResult.Fail("Draw pile is empty and cannot be replenished.");
                    drawn = _drawPile[_drawPile.Count - 1];
                }
                else // Pile
                {
                    // Taking the pile's top card. It then cannot be played this turn.
                    drawn = TopOfPile;
                }

                // The drawn card enters the hand but is barred from this turn's play.
                if (move.Played.Contains(drawn))
                    return MoveResult.Fail("The drawn card cannot be played on the same turn.");
                working.Add(drawn);
            }

            // Every played card must be available (respecting multiplicity).
            if (!IsSubMultiset(move.Played, working))
                return MoveResult.Fail("Played cards are not all in hand.");

            // Retain-one: a play may never empty the hand.
            if (working.Count - move.Played.Count < 1)
                return MoveResult.Fail("A play must leave at least one card in hand.");

            // Shape + matching. Matching is switched off when a card was drawn.
            if (!Melds.IsLegalPlay(move.Played, TopOfPile, matchingRequired: !drew))
                return MoveResult.Fail(drew ? "Played cards do not form a legal meld."
                                            : "Played meld is illegal or does not match the top card.");

            // ---- All checks passed; commit the mutation. ----
            if (drew)
            {
                if (move.Source == DrawSource.Deck)
                    _drawPile.RemoveAt(_drawPile.Count - 1);
                else
                    _discard.RemoveAt(_discard.Count - 1); // took the pile top
                hand.Add(drawn);
            }

            foreach (var c in move.Played)
                hand.Remove(c);

            // Place played cards on the pile, ending with the chosen top card so it is what the
            // next player must match.
            foreach (var c in move.Played.Where(c => c != move.TopChoice))
                _discard.Add(c);
            _discard.Add(move.TopChoice);

            AdvanceTurn();
            return MoveResult.Success();
        }

        private void AdvanceTurn()
        {
            _turnsTaken++;
            _turnIndex = (_turnIndex + 1) % _seats.Count;
        }

        /// <summary>
        /// Ensure the draw pile has at least one card, reshuffling the discard pile back in (retaining
        /// its top card) when it runs dry. Returns false only if nothing can be drawn at all.
        /// </summary>
        private bool EnsureDrawable()
        {
            if (_drawPile.Count > 0) return true;
            if (_discard.Count <= 1) return false; // only the top card remains; nothing to reshuffle

            Card top = _discard[_discard.Count - 1];
            var recycled = _discard.GetRange(0, _discard.Count - 1);
            _discard.Clear();
            _discard.Add(top);

            Deck.Shuffle(recycled, _rng);
            _drawPile.AddRange(recycled);
            return _drawPile.Count > 0;
        }

        private static bool IsSubMultiset(IReadOnlyList<Card> subset, IReadOnlyList<Card> superset)
        {
            var counts = new Dictionary<byte, int>();
            foreach (var c in superset)
                counts[c.Value] = counts.TryGetValue(c.Value, out int n) ? n + 1 : 1;
            foreach (var c in subset)
            {
                if (!counts.TryGetValue(c.Value, out int n) || n == 0) return false;
                counts[c.Value] = n - 1;
            }
            return true;
        }

        // ---------------------------------------------------------------------
        // Legal-move generation (for AI opponents and exhaustive tests).
        // ---------------------------------------------------------------------

        /// <summary>
        /// Enumerate legal moves for the current player. Not guaranteed minimal, but every returned
        /// move passes <see cref="Apply"/>. Includes declare (when allowed), no-draw plays, and
        /// draw-then-play options from both sources.
        /// </summary>
        public IEnumerable<Move> LegalMoves()
        {
            if (Finished) yield break;
            int seat = CurrentSeat;
            var hand = _hands[seat];

            if (CanDeclare(seat))
                yield return Move.Declare();

            // No-draw plays: matching required, retain-one enforced.
            foreach (var meld in EnumerateMelds(hand))
            {
                if (hand.Count - meld.Count < 1) continue;
                if (!Melds.MatchesTop(meld, TopOfPile)) continue;
                foreach (var top in meld.Distinct())
                    yield return Move.Play(meld, top);
            }

            // Draw-then-play: matching switched off. Try both sources.
            foreach (DrawSource src in new[] { DrawSource.Deck, DrawSource.Pile })
            {
                if (src == DrawSource.Deck && _drawPile.Count == 0 && _discard.Count <= 1) continue;
                Card drawn = src == DrawSource.Deck
                    ? (_drawPile.Count > 0 ? _drawPile[_drawPile.Count - 1] : default)
                    : TopOfPile;
                if (src == DrawSource.Deck && _drawPile.Count == 0) continue; // can't preview a reshuffle target cheaply

                var afterDraw = new List<Card>(hand) { drawn };
                foreach (var meld in EnumerateMelds(afterDraw))
                {
                    if (meld.Contains(drawn)) continue;              // drawn card can't be played
                    if (afterDraw.Count - meld.Count < 1) continue;  // retain-one
                    foreach (var top in meld.Distinct())
                        yield return Move.DrawPlay(src, meld, top);
                }
            }
        }

        /// <summary>All singles, sets, and sequences that can be formed from a set of cards.</summary>
        internal static IEnumerable<List<Card>> EnumerateMelds(IReadOnlyList<Card> cards)
        {
            // Singles
            foreach (var c in cards)
                yield return new List<Card> { c };

            // Sets: same rank, size >= 2 (all subsets of each rank group).
            foreach (var group in cards.Where(c => !c.IsPrintedJoker).GroupBy(c => c.Rank))
            {
                var list = group.ToList();
                if (list.Count < 2) continue;
                foreach (var subset in Subsets(list, minSize: 2))
                    yield return subset;
            }

            // Sequences: per suit, every consecutive run of length >= 3 (low- and high-ace).
            foreach (var suitGroup in cards.Where(c => !c.IsPrintedJoker).GroupBy(c => c.Suit))
            {
                var bySuit = suitGroup.ToList();
                foreach (var run in EnumerateRuns(bySuit))
                    yield return run;
            }
        }

        private static IEnumerable<List<Card>> Subsets(List<Card> items, int minSize)
        {
            int n = items.Count;
            for (int mask = 1; mask < (1 << n); mask++)
            {
                if (PopCount(mask) < minSize) continue;
                var subset = new List<Card>();
                for (int i = 0; i < n; i++)
                    if ((mask & (1 << i)) != 0) subset.Add(items[i]);
                yield return subset;
            }
        }

        private static int PopCount(int v)
        {
            int c = 0;
            while (v != 0) { v &= v - 1; c++; }
            return c;
        }

        private static IEnumerable<List<Card>> EnumerateRuns(List<Card> sameSuit)
        {
            // Map rank -> card (a suit holds at most one of each rank).
            var byRank = sameSuit.GroupBy(c => (int)c.Rank).ToDictionary(g => g.Key, g => g.First());

            // Consider both ace-low (1) and ace-high (14) placements.
            var placements = new List<Dictionary<int, Card>> { byRank };
            if (byRank.ContainsKey((int)Rank.Ace))
            {
                var high = byRank.Where(kv => kv.Key != (int)Rank.Ace).ToDictionary(kv => kv.Key, kv => kv.Value);
                high[14] = byRank[(int)Rank.Ace];
                placements.Add(high);
            }

            var seen = new HashSet<string>();
            foreach (var map in placements)
            {
                var ranks = map.Keys.OrderBy(r => r).ToList();
                for (int i = 0; i < ranks.Count; i++)
                {
                    var run = new List<Card> { map[ranks[i]] };
                    for (int j = i + 1; j < ranks.Count && ranks[j] == ranks[j - 1] + 1; j++)
                        run.Add(map[ranks[j]]);
                    // Emit every sub-run of length >= 3 starting at i.
                    for (int len = 3; len <= run.Count; len++)
                    {
                        var sub = run.GetRange(0, len);
                        string key = string.Join(",", sub.Select(c => c.Value).OrderBy(v => v));
                        if (seen.Add(key)) yield return sub;
                    }
                }
            }
        }
    }
}
