# Currency and car shop

## Current flow

- A new profile starts with 1,000 credits.
- Finishing a race grants the selected track's completion reward.
- Setting a new record grants the track's record bonus.
- Clean drift segments lasting at least one second grant coins per second, capped per race.
- The complete-race screen shows the reward breakdown and current wallet balance.
- Garage acts as the first shop: an unowned car shows `BUY`, while an owned car shows `SELECT`.

## Tuning in Unity

Select a `CarDefinition` asset in `Assets/Data` to configure:

- `Unlocked By Default`
- `Purchase Price`

Select a `TrackDefinition` asset to configure:

- `Completion Reward`
- `New Record Bonus`
- `Drift Coins Per Second`
- `Maximum Drift Reward`

The clean-drift minimum duration and collision threshold are serialized fields on `DriftRewardTracker`.

## Save keys

The MVP uses `PlayerPrefs`:

- `super_racing_currency`
- `super_racing_owned_car_<carId>`

Keep all balance mutations behind `CurrencyWallet` so the persistence backend can be replaced later.
