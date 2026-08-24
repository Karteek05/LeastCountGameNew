using System;
using System.Collections.Generic;
using System.Linq;

namespace LeastCount.Core
{
    public enum MoveType
    {
        /// <summary>Play a meld with no draw; must match the top card.</summary>
        Play,
        /// <summary>Draw one card (which cannot be played this turn), then play a meld with no match requirement.</summary>
        DrawPlay,
        /// <summary>End the round.</summary>
        Declare,
    }

    public enum DrawSource
    {
        Deck,
        Pile,
    }

    /// <summary>
    /// A single turn action. Immutable and self-contained: for a multi-card play it also carries
    /// <see cref="TopChoice"/>, the played card the player wants left on top for the next player to match.
    /// </summary>
    public sealed class Move
    {
        public MoveType Type { get; }
        public DrawSource? Source { get; }
        /// <summary>Cards being played from hand (empty for a declare).</summary>
        public IReadOnlyList<Card> Played { get; }
        /// <summary>Which played card lands on top of the discard pile. Must be one of <see cref="Played"/>.</summary>
        public Card TopChoice { get; }

        private Move(MoveType type, DrawSource? source, IReadOnlyList<Card> played, Card topChoice)
        {
            Type = type;
            Source = source;
            Played = played;
            TopChoice = topChoice;
        }

        public static Move Play(IReadOnlyList<Card> played, Card topChoice)
        {
            if (played == null || played.Count == 0) throw new ArgumentException("Play requires cards.", nameof(played));
            if (!played.Contains(topChoice)) throw new ArgumentException("TopChoice must be one of the played cards.", nameof(topChoice));
            return new Move(MoveType.Play, null, played.ToList(), topChoice);
        }

        /// <summary>Single-card play convenience.</summary>
        public static Move Play(Card single) => Play(new[] { single }, single);

        public static Move DrawPlay(DrawSource source, IReadOnlyList<Card> played, Card topChoice)
        {
            if (played == null || played.Count == 0) throw new ArgumentException("Play requires cards.", nameof(played));
            if (!played.Contains(topChoice)) throw new ArgumentException("TopChoice must be one of the played cards.", nameof(topChoice));
            return new Move(MoveType.DrawPlay, source, played.ToList(), topChoice);
        }

        public static Move DrawPlay(DrawSource source, Card single) => DrawPlay(source, new[] { single }, single);

        public static Move Declare() => new Move(MoveType.Declare, null, Array.Empty<Card>(), default);

        public override string ToString() => Type switch
        {
            MoveType.Declare => "Declare",
            MoveType.DrawPlay => $"Draw({Source}) + Play [{string.Join(" ", Played)}] top={TopChoice}",
            _ => $"Play [{string.Join(" ", Played)}] top={TopChoice}",
        };
    }
}
