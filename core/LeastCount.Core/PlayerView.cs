using System.Collections.Generic;

namespace LeastCount.Core
{
    /// <summary>
    /// What a single client is allowed to know. This is the ONLY shape the server should send to a
    /// client: it contains that player's own hand and public table state, and only the *counts* of
    /// other players' hands — never their cards.
    ///
    /// This is the structural fix for the legacy "hands broadcast to all clients" bug: there is no
    /// field here that could leak another player's cards.
    /// </summary>
    public sealed class PlayerView
    {
        /// <summary>The seat this view belongs to.</summary>
        public int Seat { get; }

        /// <summary>This player's own cards.</summary>
        public IReadOnlyList<Card> Hand { get; }

        /// <summary>Top of the discard pile — the card to match.</summary>
        public Card TopOfPile { get; }

        /// <summary>The round's cut-joker rank (scores 0).</summary>
        public Rank JokerRank { get; }

        /// <summary>Number of cards remaining in the draw pile.</summary>
        public int DrawPileCount { get; }

        /// <summary>Card counts for every seat (including self), keyed by seat. Counts only, never cards.</summary>
        public IReadOnlyDictionary<int, int> HandCounts { get; }

        /// <summary>Seat whose turn it currently is.</summary>
        public int CurrentSeat { get; }

        /// <summary>True if this player may legally declare right now.</summary>
        public bool CanDeclare { get; }

        public PlayerView(
            int seat,
            IReadOnlyList<Card> hand,
            Card topOfPile,
            Rank jokerRank,
            int drawPileCount,
            IReadOnlyDictionary<int, int> handCounts,
            int currentSeat,
            bool canDeclare)
        {
            Seat = seat;
            Hand = hand;
            TopOfPile = topOfPile;
            JokerRank = jokerRank;
            DrawPileCount = drawPileCount;
            HandCounts = handCounts;
            CurrentSeat = currentSeat;
            CanDeclare = canDeclare;
        }
    }
}
