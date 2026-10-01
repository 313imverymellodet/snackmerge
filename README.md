# SNACK MERGE

A Suika-style merge game in the browser (Unity 6 WebGL) with the Kenney Food Kit: drop snacks into the jar, two of a kind merge into the next one up, from a cherry all the way to a watermelon.

- **11-snack ladder** on 2D circle physics, rendered as 3D models that roll and tumble.
- **Combos** for chained merges, **JACKPOT** for two watermelons, a discovery ladder of every snack you've made.
- **CLASSIC** (random drops) and **DAILY JAR** (everyone gets the same seeded drops today) with their own leaderboards.
- **Powers**: one free SHAKE and POP per game, plus a CONTINUE; more via rewarded ads on portals (Poki / CrazyGames).
- Mouse or touch to aim, release to drop; keyboard arrows/A-D + Space.

Backend: the shared `orbyt` server (`server/snack.js`).

## Build

```
"C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe" -batchmode -nographics -projectPath unity -executeMethod SnackBuild.WebGL -quit -logFile build.log
node tools/serve.mjs 8100
```

Assets: Kenney Food Kit (CC0).
