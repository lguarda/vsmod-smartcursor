# Changelog
## [0.0.4] - 2026-01-07
### Fixed
- Before the World.Player.InventoryManager.TryTransferTo was used to transfer swap item passing by the mouse slot, first this was shitty code then it cause weird bug with
  some item (bucket filled with liquid), i changed it to IInventory.TryFlipItems, mouse slot is no longer used as temporary slot, and the bug don't exist anymore
### Added
- Now if you keep the key pressed and look at different block it will continue to update the tool in your hand.
- `continuousMode` was added in `smartcursor.json` to control this behavior it's `true` by default
- New keybind was added `smartcursor one shot` it's not set by default, when pressed it will swap the tool but it's permanent (not really permanent but the mod will not do it for you)

## [0.0.3] - 2026-01-07
### Added
- Now the mod will create a smartcursor.json file in VintagestoryData/ModConfig which can be modified to change the behavior
- There's a domain tools selection in the configuration, so for example even if mushroom are "Plant", you can force the mod to pop the knife on every mushroom instead of scythe (i don't know why but it's called domain)
  another example anvil are "Metal" block but with anvil domain so in the default config right Now anvil has ["pickaxe", "hammer"], it can be reverted to always pop the hammer first when looking at the anvil.
### Known issue
- When you have bowl or bucket, with anther bowl/bucket filed with anything, the inner used api.World.Player.InventoryManager.TryTransferTo seems to have a bug where it will move the other bowl/bucket into the moved slot so the mod stop and you will be left with the bowl/buckt in your mouse slot

## [0.0.2] - 2026-01-06
### Changed
- On metal block use the hammer instead of pickaxe
## Added
- Now multi tools is supported example for leave its prioritize Shears than Axe, or for plant Scythe than Knife, the goal is to make this configurable later
- Now support Knife on dead corps

## [0.0.1] - 2026-01-02
### Added
- initial release with base working principle
