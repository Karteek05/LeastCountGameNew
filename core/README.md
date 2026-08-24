# LeastCount.Core

The **authoritative Least Count rules engine** as a plain C# library with **zero Unity dependencies**.
Per `CLAUDE.md`, this is the code the Unity client *and* the future Azure server both consume, so
the rules are written once and validated server-side.

```
core/
├── LeastCount.sln
├── LeastCount.Core/            # netstandard2.0 — Unity + server both consume this
│   ├── Cards.cs                # wire encoding (bytes 0-51 standard, 52+ printed jokers)
│   ├── Scoring.cs              # joker-aware point values (NOT card rank)
│   ├── Deck.cs                 # deck build, deterministic shuffle, 2-card round setup
│   ├── Melds.cs                # single / set / sequence classification + match rules
│   ├── Moves.cs                # Play / Draw+Play / Declare move model
│   ├── PlayerView.cs           # leak-free per-client projection (no other hands)
│   ├── RoundEngine.cs          # authoritative turn loop, validation, deck-exhaustion reshuffle
│   ├── DeclareScoring.cs       # declare outcomes incl. 20-pt wrong-declare penalty
│   └── Match.cs                # cumulative scores, elimination at 200, LOWEST wins
├── LeastCount.Core.Tests/      # net10.0, xUnit — 57 tests
└── LeastCount.Cli/             # console app that auto-plays a full match on the engine
```

## Build & test

```bash
cd core
dotnet test                         # 57 tests
dotnet run --project LeastCount.Cli -- 4 7   # watch a 4-player match (seed 7)
```

## Legacy bugs fixed by construction

| Legacy bug (see CLAUDE.md) | How the core avoids it |
|---|---|
| Winner was the **highest** hand | `Match` / `LowestSeat` — lowest wins; regression-tested |
| Scoring summed raw wire bytes (K♠ ≠ K♥) | `Scoring.PointValue` maps rank→points; regression-tested |
| No joker-rank awareness | Every scoring path takes the round's `jokerRank`; that rank scores 0 |
| Printed-joker arithmetic garbage | `Card.IsPrintedJoker` checked before any `v/4` / `v%4` |
| `PLAYER_INITIAL_CARDS = 5` | Deal count is a parameter; setup deals 7 in tests |

## Open rules questions — current defaults

These are flagged in `Melds.cs` and easy to flip once confirmed:

- Cut-joker-rank card is **not** a sequence wildcard.
- A cut-joker-rank card matches only its **printed** suit/rank when played (no wildcard match).
- Drawing from the pile takes the **top card only** (enforced at the round-engine layer, TBD).

## Built and running

- Full turn engine (`RoundEngine`): action → draw source → card selection → which card lands on top,
  with "drawn card can't be played this turn", "retain ≥1 card", and declare-timing all enforced.
- Draw-pile exhaustion: reshuffles the discard pile back in, retaining the top card.
- Legal-move generation for bots/tests; a greedy bot drives the CLI through complete matches.

## Not yet built (next increments)

- A real UI: rebuild the client (Unity or otherwise) as a thin layer over `RoundEngine`/`PlayerView`.
- The Azure WebSocket server: host `RoundEngine` authoritatively, send each client only its `PlayerView`.
- A stronger opponent AI than the greedy demo bot.
