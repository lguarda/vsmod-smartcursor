# Changelog
## [0.0.3] - XXX
## Added
- Now the mod will create a smartcursor.json file in VintagestoryData/ModConfig which can be modified to change the behavior
- There's a domain tools selection in the configuration, so for example even if mushroom are "Plant", you can force the mod to pop the knife on every mushroom instead of scythe (i don't know why but it's called domain)
  another example anvil are "Metal" block but with anvil domain so in the default config right Now anvil has ["pickaxe", "hammer"], it can be reverted to always pop the hammer first when looking at the anvil.
## Known issue
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
