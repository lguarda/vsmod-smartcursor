![No shame it's chatgpt](logo.png)

# SmartCursor Vs mod
This is a Vintagestory client side mode which aim to implement the smart cursor feature from Terraria (In Terraria it's actually named smart cursor)
There's multiple "module" within this mod
- smartcuror (the original terraria idea)
- slot refill (idea from part of Jeb's Inventory Tweaks https://mods.vintagestory.at/jebsit)
- transfer away (and Put it in the bag https://mods.vintagestory.at/show/mod/31020)

So slot refill part will refill your hotbar with stuff from your inventory, like you put block on the floor it ran out on active slot, slot refill will look at you other slot to refill your current one with same item.
then transfer away add hot key to put active slot item in your bag.
Those two module can be enabled in `smartcursor.json` by setting those to true
- putitinthebag
- slotRefill
And for the WHY did i shamelessly copy some mod's behavior, is firstly because those two other mod don't work well together but worst
they don't work well with mine.
Secondly because i can't live without those.
Thirdly they kind of already fit the purpose of my own mod, at least for the refill part.

# How smartcursor module works
SmartCursor automatically selects the most appropriate tool(or item) based on what you are looking at.

When the keybind is pressed, the mod analyzes:
- the targeted entities (corpes, firepit)
- the targeted block

Based on this analysis, it determines the preferred tool
(axe, pickaxe, hammer, knife, shovel, scythe, shears, etc.).

![wa](vsmod-smartcursor-full-demo.gif)
![aaaa](demo/vsmod-smartcursor-demo-inventory-swap.gif)

### Tool selection order
1. there's some hard-coded stuff like dead entity pop the knife and worked clay will pop the need clay type (only when present of course)
2. Domain-based overrides (configured in `smartcursor.json`)
   - Example: mushrooms always use Knife (no Scythe)
   - Example: anvils prioritize Hammer over Pickaxe
3. Block material rules (Metal, Stone, Plant, Leaves, etc.) which take the best suited tool from you inventory

### Inventory lookup
- The hotbar is scanned first
- If no matching tool is found, the main inventory is scanned
- The first matching tool based on priority is selected
- order can be configured with field inventories in `smartcursor.json`
  also other mods inventory could be supported example by default salty's toolbelt is supported

### Swapping behavior
- The selected tool is swapped with the currently active hotbar slot
- Swapping is done using inventory-native flip logic, avoiding item desync issues

### Modes
- **Hold mode**: (default: 'R')
  While the key is held, the tool in hand updates dynamically as you look at different blocks or entities.
- **Toggle mode**: (default 'alt+R')
  Press once to activate SmartCursor, press again to restore the previous item.
- **One-shot mode**: (default 'None')
  Press once to select the correct tool and keep it; SmartCursor will not swap it back automatically.

### Restore behavior
- When SmartCursor deactivates (key released or toggle off), the original item is restored to its original slot.

### Blacklist
- You can black list item in the current active hotbar slot by pressing a key (default: <ctrl+alt+R>) it will toggle on and off black list for this item
  it can be useful for quest item ex: "Tin bronze pickaxe" if you want to be sure the mod will not pop this item.
  It can also be used if the mod has a bug or if other mod as some issue, for example the mod walkingstick the item property is tagged as pickaxe, so the mod can pop them, so you can simply black list it.

### Configuration
- A `smartcursor.json` file is created in `VintagestoryData/ModConfig`
- Behavior such as continuous updating and tool priorities can be customized

### Extended rules
configurable with field `extendedRule` in `smartcursor.json`
Smartcursor is controlled by different rules within src/rules/
Those one was the original rule when i release the mode so there are just always turned ON
- ToolRule.cs (find tool, like shovel if you look a dirt)
- ClayFormingRule.cs (find matching clay when looking at clay forming)
- LiveEntityRule.cs (find the knife when looking at dead corps)
Then extendedRule which is ON by default will add:
- BloomeryRule.cs (based current bloomery state, sequentially find nuggets, fuel, torch)
- CrockRule.cs (find beewax or bowl)
- PitKilnRule.cs (based current pitkiln state, sequentially find drygrass, stick fuel, torch)
- TorchRule.cs (looking at unlit torch will bring at lit torch)
- TroughRule.cs (find grain or mash to put into trough)
- PipetteRule.cs (find same item you are looking this one has it's own configurable keybind)


### Current status
This mod is experimental.
The current state is **"it works for me"** — use at your own risk.

# Build & run
first I'm pretty sure this doesn't work on windows yet
It's built like this:

    # This need to be ran only once
    scons VINTAGE_STORY=$(realpath <path to vs>) VINTAGE_STORY_DATA=$(realpath <your vs data location>)

    # default value for VINTAGE_STORY_DATA is ~/.config/VintagestoryData/ so you may not need this

    # than
    scons install run
    scons # or simply scons for build only

# Format
    scons fmt

# Test
Build vsqa

    scons vsqa run

then In game type /runtests

