using System;
using System.Collections.Generic;

namespace LeastCount.Core
{
    /// <summary>
    /// Abstraction over randomness so shuffles are deterministic (and therefore testable).
    /// The server owns the real RNG; tests inject a seeded one.
    /// </summary>
    public interface IRng
    {
        /// <summary>Returns an int in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }

    /// <summary>Seeded, reproducible <see cref="IRng"/> backed by System.Random.</summary>
    public sealed class SeededRng : IRng
    {
        private readonly Random _random;
        public SeededRng(int seed) => _random = new Random(seed);
        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }

    /// <summary>Deck construction, shuffling, and the Least Count two-card round setup.</summary>
    public static class Deck
    {
        /// <summary>A fresh, ordered deck: bytes 0..51 then the printed jokers.</summary>
        public static List<Card> BuildFullDeck()
        {
            var deck = new List<Card>(Card.DeckSize);
            for (int v = 0; v < Card.DeckSize; v++)
                deck.Add(new Card((byte)v));
            return deck;
        }

        /// <summary>In-place Fisher-Yates shuffle using the injected RNG.</summary>
        public static void Shuffle(IList<Card> cards, IRng rng)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }

        /// <summary>
        /// Deals a full round from a freshly shuffled deck:
        ///  - <paramref name="playerCount"/> hands of <paramref name="cardsPerPlayer"/> cards each;
        ///  - a cut-joker card whose RANK is the round's joker (all suits of that rank score 0);
        ///  - an opening card that starts the discard pile.
        /// Neither the cut joker nor the opening card may be a printed joker; such cards are
        /// discarded (removed from play) and the next card is turned.
        /// Remaining cards form the draw pool.
        /// </summary>
        public static RoundDeal Deal(int playerCount, int cardsPerPlayer, IRng rng)
        {
            if (playerCount < 2) throw new ArgumentOutOfRangeException(nameof(playerCount), "Need at least 2 players.");
            if (cardsPerPlayer < 1) throw new ArgumentOutOfRangeException(nameof(cardsPerPlayer));

            var deck = BuildFullDeck();
            Shuffle(deck, rng);

            int need = playerCount * cardsPerPlayer + 2; // + cut joker + opening (before any re-draws)
            if (deck.Count < need)
                throw new InvalidOperationException(
                    $"Deck of {deck.Count} too small to deal {cardsPerPlayer} to {playerCount} players.");

            // Draw from the end of the list (cheap removal); order within a hand doesn't matter.
            int next = deck.Count - 1;
            Card Take() => deck[next--];

            var hands = new List<Card>[playerCount];
            for (int p = 0; p < playerCount; p++)
                hands[p] = new List<Card>(cardsPerPlayer);

            // Deal round-robin so a re-shuffle biases no single seat.
            for (int c = 0; c < cardsPerPlayer; c++)
                for (int p = 0; p < playerCount; p++)
                    hands[p].Add(Take());

            var burned = new List<Card>();

            Card cutJoker = TurnNonJoker(Take, ref next, burned);
            Card opening = TurnNonJoker(Take, ref next, burned);

            // Whatever's left (indices 0..next) is the draw pool.
            var pool = new List<Card>(next + 1);
            for (int i = 0; i <= next; i++)
                pool.Add(deck[i]);

            return new RoundDeal(hands, cutJoker, opening, pool, burned);
        }

        private static Card TurnNonJoker(Func<Card> take, ref int next, List<Card> burned)
        {
            while (true)
            {
                Card c = take();
                if (!c.IsPrintedJoker) return c;
                burned.Add(c); // printed joker can't set the joker rank or open the pile; remove it.
            }
        }
    }

    /// <summary>Immutable result of dealing a round.</summary>
    public sealed class RoundDeal
    {
        /// <summary>Each player's starting hand, indexed by seat.</summary>
        public IReadOnlyList<IReadOnlyList<Card>> Hands { get; }

        /// <summary>The turned cut-joker card. Its RANK is the round's joker rank.</summary>
        public Card CutJoker { get; }

        /// <summary>Round joker rank derived from the cut joker.</summary>
        public Rank JokerRank => CutJoker.Rank;

        /// <summary>Card that opens the discard pile.</summary>
        public Card Opening { get; }

        /// <summary>Remaining draw pool (top of pool = last element).</summary>
        public IReadOnlyList<Card> Pool { get; }

        /// <summary>Printed jokers removed while turning the cut/opening cards.</summary>
        public IReadOnlyList<Card> Burned { get; }

        public RoundDeal(
            IReadOnlyList<IReadOnlyList<Card>> hands,
            Card cutJoker,
            Card opening,
            IReadOnlyList<Card> pool,
            IReadOnlyList<Card> burned)
        {
            Hands = hands;
            CutJoker = cutJoker;
            Opening = opening;
            Pool = pool;
            Burned = burned;
        }
    }
}
