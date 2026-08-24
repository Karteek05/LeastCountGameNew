using System;

namespace LeastCount.Core
{
    /// <summary>
    /// Suit values match the legacy wire encoding (v % 4): Spades=0, Clubs=1, Diamonds=2, Hearts=3.
    /// <see cref="None"/> is used for printed jokers, which have no suit.
    /// </summary>
    public enum Suit
    {
        None = -1,
        Spades = 0,
        Clubs = 1,
        Diamonds = 2,
        Hearts = 3,
    }

    /// <summary>
    /// Rank values match the legacy wire encoding (v / 4 + 1): Ace=1 .. King=13.
    /// <see cref="Joker"/> is the printed joker, which has no playing rank.
    /// NOTE: this is the card's RANK, not its point value. Point values depend on the
    /// round's cut-joker rank and must go through <see cref="Scoring"/>.
    /// </summary>
    public enum Rank
    {
        None = -1,
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        /// <summary>Printed joker. Sorts above King so ordering stays stable.</summary>
        Joker = 14,
    }

    /// <summary>
    /// Immutable value type over a single wire byte. This byte encoding IS the network protocol
    /// (see CLAUDE.md): bytes 0-51 are the standard deck, printed jokers occupy 52+.
    /// Changing this after release breaks old clients against the server, so treat it as fixed.
    /// </summary>
    public readonly struct Card : IEquatable<Card>, IComparable<Card>
    {
        /// <summary>First byte value used for printed jokers. Everything below this is a standard card.</summary>
        public const byte FirstPrintedJoker = 52;

        /// <summary>
        /// Number of printed jokers in the deck. A standard retail pack ships 2 playable jokers
        /// (a third "advertisement/guarantee" card is not a playing card and is excluded).
        /// </summary>
        public const int PrintedJokerCount = 2;

        /// <summary>Total cards in the deck: 52 standard + printed jokers.</summary>
        public const int DeckSize = FirstPrintedJoker + PrintedJokerCount; // 54

        /// <summary>Sentinel returned when the draw pool is exhausted. Never a real card.</summary>
        public const byte PoolIsEmpty = 255;

        public byte Value { get; }

        public Card(byte value)
        {
            if (value >= DeckSize && value != PoolIsEmpty)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    $"Card byte must be 0..{DeckSize - 1} or the {PoolIsEmpty} sentinel.");
            Value = value;
        }

        /// <summary>True if this byte encodes a printed joker (no suit, no rank). Must be checked
        /// BEFORE the v/4 and v%4 arithmetic, which is only valid for the 52 standard cards.</summary>
        public bool IsPrintedJoker => Value >= FirstPrintedJoker && Value != PoolIsEmpty;

        public Rank Rank => IsPrintedJoker ? Rank.Joker : (Rank)(Value / 4 + 1);

        public Suit Suit => IsPrintedJoker ? Suit.None : (Suit)(Value % 4);

        /// <summary>Static form of <see cref="Rank"/>, kept for parity with the legacy Card.GetRank API.</summary>
        public static Rank GetRank(byte value) => new Card(value).Rank;

        /// <summary>Static form of <see cref="Suit"/>, kept for parity with the legacy Card.GetSuit API.</summary>
        public static Suit GetSuit(byte value) => new Card(value).Suit;

        public bool Equals(Card other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is Card c && Equals(c);
        public override int GetHashCode() => Value;
        public int CompareTo(Card other) => Value.CompareTo(other.Value);

        public static bool operator ==(Card a, Card b) => a.Value == b.Value;
        public static bool operator !=(Card a, Card b) => a.Value != b.Value;

        public override string ToString()
        {
            if (IsPrintedJoker) return "Joker";
            string r = Rank switch
            {
                Rank.Ace => "A",
                Rank.Jack => "J",
                Rank.Queen => "Q",
                Rank.King => "K",
                _ => ((int)Rank).ToString(),
            };
            string s = Suit switch
            {
                Suit.Spades => "♠",
                Suit.Clubs => "♣",
                Suit.Diamonds => "♦",
                Suit.Hearts => "♥",
                _ => "?",
            };
            return r + s;
        }
    }
}
