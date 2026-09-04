# Currency and car shop

## Current flow

- A new profile starts with 1,000 credits.
- Finishing a race grants the selected track's completion reward.
- Setting a new record grants the track's record bonus.
- Clean drift segments lasting at least one second grant coins per second, capped per race.
- The complete-race screen shows the reward breakdown and current wallet balance.
- Garage sells cars: an unowned car shows `BUY`, while an owned car shows `SELECT`.
- Owned cars show upgrade buttons directly beside their stat bars and a paint strip above the select button.
- Buying a car, upgrade, or new paint always requires confirmation before credits are deducted.
- Every upgrade and paint purchase is stored separately for each car.
- Performance upgrades have five levels and move the car's percentage toward 100%; they never exceed the controller defaults.
- Paint index 0 is the factory paint and preserves the prefab's original material color.

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

Additional per-car keys:

- `super_racing_upgrade_<carId>_<upgradeType>`
- `super_racing_paint_owned_<carId>_<paintIndex>`
- `super_racing_paint_equipped_<carId>`
