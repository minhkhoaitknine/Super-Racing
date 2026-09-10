# Global leaderboards

The game signs in anonymously through UGS. No player login screen or backend server is required.
The Authentication SDK restores its cached session on subsequent launches. Losing that cache
(for example clearing game data) loses access to an unlinked guest account.

## Cloud configuration

`Assets/Resources/GlobalLeaderboards.json` maps the track IDs to UGS boards in `production`:

| Track | Board | Race length |
| --- | --- | --- |
| beach | map1 | 1 lap |
| desert | map2 | 1 lap |
| town_square | map3 | 4 laps |

Each board must use **Ascending**, **Best Score**, **no buckets**, and **no scheduled resets**.
Scores are integer milliseconds for the entire completed race, across all cars.
If race length or track layout changes, use a new board ID to keep results comparable.
Prototype/test tracks have no mapping and do not upload.

## In game

- Finish a race: the existing finish flow saves the local record and queues a global result.
- A persistent service uploads the fastest pending result per board. Network failures do not
  stop the race, rewards, or scene transition. Pending results persist in PlayerPrefs and retry
  every 30 seconds or when opening/refreshing the leaderboard.
- Select **GLOBAL RANKING** on track selection or race completion to see up to 100 global
  entries and your own rank even when outside the top 100. Scroll for additional entries.
- Race completion displays the sync status. Global times display milliseconds.
- Names use the UGS player name when available, otherwise a short `Racer <id>` label.

The queue is scoped to Cloud project ID and environment. Do not clear all PlayerPrefs in normal
game flow because the anonymous session and pending records are device-local.
There are no admin credentials in the game. Client-submitted times are not cheat-proof.

## Verification

EditMode tests cover invalid times, millisecond conversion, durable best-only queue behavior,
map routing, and time formatting. Existing record and finish checkpoint tests are also run.
Live checks signed in anonymously and read map1/map2/map3 successfully. These checks did not
insert fabricated records into production; verify a real completed race uploads before release.
