# SNACK MERGE: portal submission kit

Everything needed to put SNACK MERGE on CrazyGames, Poki and GameDistribution.
Developer accounts and submissions are yours to do. Each portal makes you accept its own terms.

## 1. Build the packages

```bash
node tools/portals.mjs --gd YOUR_GD_GAME_ID
```

This writes three folders and three zips to `portals/` (gitignored). `index.html` sits at the root of each zip.

| Portal | Zip | SDK inside |
|---|---|---|
| CrazyGames | `portals/snackmerge-crazygames.zip` | CrazyGames SDK v3 |
| Poki | `portals/snackmerge-poki.zip` | Poki SDK v2 |
| GameDistribution | `portals/snackmerge-gd.zip` | GD HTML5 SDK (needs your Game ID) |

Each zip is about 5.5 MB and runs from any static host. The Unity files decompress in the browser, so no special server headers are needed.

## 2. What each build does

- **Midgame ad** before each new round, through the portal's own call.
  - Poki gets `commercialBreak` before every round and rate-limits itself.
  - CrazyGames and GD skip the first round of a session and keep at least 60 s between ads.
- **Rewarded ads** (opt-in only):
  - CONTINUE after the jar fills (once per game).
  - An extra SHAKE or POP once the free one is used.
- **During any ad** the game freezes and mutes, then resumes when the SDK hands back control. GD's own pause/resume events are handled too.
- **Gameplay events**:
  - Loading start/stop and gameplay start/stop, including stop on pause and resume after.
  - CrazyGames `happytime()` on a watermelon or a new big snack.
- **Portal rules covered**:
  - No outbound links; SHARE is hidden in portal builds.
  - No PWA or OG tags.
  - Space, arrow keys and the mouse wheel cannot scroll the portal page.
- **CrazyGames sign-in**: signed-in players get their username pre-filled on the leaderboard name prompt.
- **Analytics**: each portal reports as its own game id (`snackmerge_cg`, `snackmerge_poki`, `snackmerge_gd`), so each storefront has its own funnel. See section 6.

## 3. Art (in `portal-art/`)

| File | Use |
|---|---|
| `cg-landscape-1920x1080.png` | CrazyGames landscape cover |
| `cg-portrait-800x1200.png` | CrazyGames portrait cover |
| `cg-square-800x800.png` | CrazyGames square cover |
| `gd-512x512.png` | GameDistribution 512×512 |
| `gd-512x384.png` | GameDistribution 512×384 |
| `gd-200x120.png` | GameDistribution 200×120 |

Poki makes its own thumbnails during onboarding, so send them the 1920×1080 if they ask.

## 4. Store listing copy

**Title:** SNACK MERGE

**Short description (≤ 100 chars):**
Drop snacks in the jar and merge two of a kind, from cherries all the way to a watermelon!

**Description:**
SNACK MERGE is a juicy drop-and-merge puzzle. Aim, drop, and watch two matching snacks squish into a bigger one: cherry → strawberry → lemon → apple → orange → donut → coconut → pizza → cake → pumpkin → WATERMELON.

Chain merges for combo points and try not to let the jar overflow. Stuck? SHAKE the jar to settle the pile, or POP any snack to clear space.

- Classic mode: go for your best score.
- Daily Jar: the same snack order for everyone, every day. Climb the daily leaderboard.
- Discover all 11 snacks.

**Controls:**
- Mouse: move to aim, click to drop.
- Touch: drag to aim, let go to drop.
- Esc: pause.

**Tags / categories:** Puzzle, Merge, Casual, Physics, Fruit, Suika, Watermelon, Drop, 2048-style, Food, Relaxing, One-handed, Mobile

**Orientation:** both (portrait and landscape layouts).

**Platforms:** desktop and mobile.

**Age:** everyone. No violence, no chat. Leaderboard names are filtered server-side.

**Languages:** English.

## 5. Portal steps

### CrazyGames
1. Create a developer account at developer.crazygames.com.
2. Submit a new game as **HTML5**:
   - Upload `snackmerge-crazygames.zip`.
   - Add the three `cg-*` covers and the listing copy above.
3. Test it in their preview (QA tool). The SDK runs in "local" mode on your own machine and shows placeholder ads.
4. New games start with a limited test launch. They roll out wider based on play time and retention, so the first-30-seconds work in this build matters.

### Poki
1. Apply at developers.poki.com.
2. Poki reviews games before onboarding. Share `https://snackmerge.vercel.app` as the playable link, and the zip if they ask for a build.
3. If accepted, they put the Poki build through their inspector; it already calls `gameLoadingFinished`, `gameplayStart`/`Stop`, `commercialBreak` and `rewardedBreak`.

### GameDistribution
1. Create a developer account at developer.gamedistribution.com and add a new game. It gives you a **Game ID**.
2. Rebuild with it:

   ```bash
   node tools/portals.mjs --gd THAT_ID
   ```

   Or send me the ID and I'll do it.
3. Upload `snackmerge-gd.zip`, the three `gd-*` images and the listing copy.
4. **Turn on rewarded ads** for the game in the GD dashboard. Without it, the rewarded calls time out and players simply don't get the extra.

## 6. Watching the numbers

All builds send anonymous event counts to our server, readable with the admin stats call.

**Funnel events, in order:**
1. `page_view`
2. `start_classic` / `start_daily`
3. `tut_aimed` (2 drops)
4. `tut_merged` (first merge)
5. `drops_10/25/50/100/200`
6. `reach_<snack>`
7. `over_classic` / `over_daily`, with `over_seconds` and `over_tier_<snack>`
8. `play_again`

**Quit points:**
- `quit_midgame` (pause → menu, value = drops).
- `over_before_merge`: the player lost before ever merging, which means the tutorial didn't land.

**Ads:**
- `ad_midgame` and `ad_rewarded`: ad requests.
- `rewarded_ok` / `rewarded_fail`: rewarded results.
- What players spent rewards on: `continue_ad`, `shake_ad`, `pop_ad`.

**Healthy early signs:**
- `tut_merged` / `start_classic` > 85%.
- `play_again` / `over_classic` > 40%.
- Median `over_seconds` > 180.
