using System.Collections.Generic;
using System.Linq;
using LeastCount.Core;

namespace LeastCount.Cli
{
    /// <summary>
    /// A deliberately simple bot: shed as many points as possible each turn, and declare once its
    /// hand is cheap enough. Good enough to drive a full match end-to-end on the real rules; it is
    /// NOT meant to be a strong opponent.
    /// </summary>
    public sealed class GreedyStrategy
    {
        private readonly int _declareThreshold;
        private const int UnknownDeckDrawPenalty = 6; // an average-ish incoming card

        public GreedyStrategy(int declareThreshold = 8) => _declareThreshold = declareThreshold;

        public Move Choose(RoundEngine engine, int seat)
        {
            Rank joker = engine.JokerRank;
            int handValue = Scoring.HandValue(engine.HandOf(seat), joker);

            if (engine.CanDeclare(seat) && handValue <= _declareThreshold)
                return Move.Declare();

            var candidates = engine.LegalMoves().Where(m => m.Type != MoveType.Declare).ToList();
            if (candidates.Count == 0)
                return engine.CanDeclare(seat) ? Move.Declare() : engine.LegalMoves().First();

            Move best = candidates[0];
            (int reduction, int drawPenalty, int shed) bestScore = Score(candidates[0]);
            foreach (var m in candidates.Skip(1))
            {
                var s = Score(m);
                if (Better(s, bestScore)) { best = m; bestScore = s; }
            }
            return best;

            (int reduction, int drawPenalty, int shed) Score(Move m)
            {
                int shed = m.Played.Sum(c => Scoring.PointValue(c, joker));
                if (m.Type == MoveType.Play)
                    return (shed, 0, shed);

                int added = m.Source == DrawSource.Pile
                    ? Scoring.PointValue(engine.TopOfPile, joker)
                    : UnknownDeckDrawPenalty;
                return (shed - added, 1, shed);
            }

            // Prefer bigger net point reduction, then no-draw over draw, then more raw points shed.
            static bool Better((int reduction, int drawPenalty, int shed) a, (int reduction, int drawPenalty, int shed) b)
            {
                if (a.reduction != b.reduction) return a.reduction > b.reduction;
                if (a.drawPenalty != b.drawPenalty) return a.drawPenalty < b.drawPenalty;
                return a.shed > b.shed;
            }
        }
    }
}
