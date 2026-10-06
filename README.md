# SNACK MONSTER

A merge game with a hungry monster (Unity 6 WebGL, Kenney Food Kit). Drop snacks into the jar; two of a kind merge into the next one up. A monster leans on the jar craving one snack at a time: merge into that snack and its tongue snatches it out of the jar for big points (and room to breathe). Keep it waiting and it gets grumpy and stomps the jar.

- **The monster** (`Monster.cs`, built from code): craving bubble with a patience ring, eyes that follow your snack, tongue grab, chomp, grows with every meal; cravings climb the ladder as it eats.
- **Special drops**: HOT PEPPER (blows up small snacks nearby) and SPRINKLE CUPCAKE (merges with whatever it touches).
- **11-snack ladder** on 2D circle physics with 3D models; combos; two watermelons = JACKPOT.
- **CLASSIC** and **DAILY JAR** (same seeded drops for everyone; the monster rolls on its own stream so the daily stays fair), each with its own leaderboard.
- **Powers**: one free SHAKE and POP per game, plus a CONTINUE; more via rewarded ads on portals.
- Mouse or touch to aim, release to drop; keyboard arrows/A-D + Space.

Backend: the shared `orbyt` server (`server/snack.js`).

## Build

```
"C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe" -batchmode -nographics -projectPath unity -executeMethod SnackBuild.WebGL -quit -logFile build.log
node tools/serve.mjs 8100
```

Assets: Kenney Food Kit (CC0).
