using System;
using System.Collections.Generic;
using System.Linq;
using LeastCount.Core;

namespace LeastCount.Cli
{
    /// <summary>
    /// Auto-plays a full Least Count match on the LeastCount.Core rules engine so the whole thing can
    /// be watched running without Unity. Usage: dotnet run -- [players] [seed]
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            int players = args.Length > 0 && int.TryParse(args[0], out int p) ? p : 4;
            int seed = args.Length > 1 && int.TryParse(args[1], out int s) ? s : 20260824;
            players = Math.Clamp(players, 2, 6);

            var names = new[] { "Aisha", "Bharat", "Chen", "Diego", "Emeka", "Farah" };
            var rng = new SeededRng(seed);
            var strategy = new GreedyStrategy();

            var seats = Enumerable.Range(0, players).ToList();
            var match = new Match(seats);

            Console.WriteLine($"Least Count — {players} players, seed {seed}");
            Console.WriteLine("Rules engine: LeastCount.Core (zero Unity dependencies)\n");

            int round = 0;
            while (!match.IsOver && round < 100)
            {
                round++;
                var active = match.ActiveSeats.ToList();
                if (active.Count < 2) break;

                var deal = Deck.Deal(active.Count, 7, rng);
                var engine = new RoundEngine(active, deal, rng);

                Console.WriteLine($"── Round {round} ─ joker rank: {RankName(engine.JokerRank)} " +
                                  $"(all {RankName(engine.JokerRank)}s score 0) ─ opening {deal.Opening}");

                int turns = 0;
                while (!engine.Finished && turns < 2000)
                {
                    turns++;
                    int seat = engine.CurrentSeat;
                    Move move = strategy.Choose(engine, seat);
                    int before = engine.HandOf(seat).Count;

                    var result = engine.Apply(seat, move);
                    if (!result.Ok)
                    {
                        Console.WriteLine($"   !! {names[seat]} attempted an illegal move: {result.Error}");
                        // Fall back to any legal move to keep the demo moving.
                        move = engine.LegalMoves().First();
                        engine.Apply(seat, move);
                    }

                    Console.WriteLine($"   {names[seat],-7} {Describe(move, engine.JokerRank)}"
                                      + (move.Type == MoveType.Declare ? "" : $"  (hand {before}→{engine.HandOf(seat).Count})"));
                }

                if (engine.DeclarerSeat is int declarer)
                    ReportRound(engine, match, names, declarer);
                else
                    Console.WriteLine("   (round ended without a declare — stalemate safeguard)\n");
            }

            Console.WriteLine("══════════════════════════════════════");
            if (match.Winner is int w)
                Console.WriteLine($"WINNER: {names[w]} (last player standing).");
            else
                Console.WriteLine("Match ended without a single winner.");
            Console.WriteLine("Final totals: " + string.Join(", ",
                match.Totals.OrderBy(kv => kv.Value).Select(kv => $"{names[kv.Key]} {kv.Value}"
                    + (match.IsEliminated(kv.Key) ? " (out)" : ""))));
            return 0;
        }

        private static void ReportRound(RoundEngine engine, Match match, string[] names, int declarer)
        {
            var scores = DeclareScoring.Score(engine.AllHands(), declarer, engine.JokerRank);
            Console.WriteLine($"   → {names[declarer]} DECLARES.");
            foreach (var sc in scores)
            {
                string tag = sc.IsDeclarer ? "declarer" : sc.IsLowest ? "lowest" : "";
                Console.WriteLine($"      {names[sc.Seat],-7} hand {sc.HandValue,3}  +{sc.Points,3}  {tag}");
            }
            match.ApplyRound(scores);
            Console.WriteLine("      totals: " + string.Join(", ",
                match.Totals.OrderBy(kv => kv.Key).Select(kv => $"{names[kv.Key]} {kv.Value}"
                    + (match.IsEliminated(kv.Key) ? " OUT" : ""))) + "\n");
        }

        private static string Describe(Move m, Rank joker) => m.Type switch
        {
            MoveType.Declare => "declares",
            MoveType.DrawPlay => $"draws {m.Source} then plays {Cards(m.Played)} (top {m.TopChoice})",
            _ => $"plays {Cards(m.Played)} (top {m.TopChoice})",
        };

        private static string Cards(IReadOnlyList<Card> cards) => string.Join(" ", cards);

        private static string RankName(Rank r) => r switch
        {
            Rank.Ace => "Ace", Rank.Jack => "Jack", Rank.Queen => "Queen", Rank.King => "King",
            _ => ((int)r).ToString(),
        };
    }
}
