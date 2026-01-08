![No shame it's chatgpt](logo.png)

# SmartCursor Vs mod
This is a Vintagestory client side mode which aim to implement the smart cursor feature from Terraria (In Terraria it's actually named smart cursor)

# How it Works
SmartCursor automatically selects the most appropriate tool based on what you are looking at.

When a keybind is pressed, the mod analyzes:
- the targeted entity (if any currently only support dead corps to pop the knife)
- otherwise the targeted block

Based on this analysis, it determines the preferred tool
(axe, pickaxe, hammer, knife, shovel, scythe, shears, etc.).

![wa](demo/vsmod-smartcursor-demo.gif)
![zaa](demo/vsmod-smartcursor-continuous-demo.gif)
![aaaa](demo/vsmod-smartcursor-demo-inventory-swap.gif)

### Tool selection order
1. Domain-based overrides (configured in `smartcursor.json`)
   - Example: mushrooms always use Knife
   - Example: anvils prioritize Hammer over Pickaxe
2. Block material rules (Metal, Stone, Plant, Leaves, etc.)
3. Not yet implemented ~Entity rules (alive vs dead entities)~

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
- **Toggle mode**: (default '`')
  Press once to activate SmartCursor, press again to restore the previous item.
- **One-shot mode**: (default 'unbound')
  Press once to select the correct tool and keep it; SmartCursor will not swap it back automatically.

### Restore behavior
- When SmartCursor deactivates (key released or toggle off), the original item is restored to its original slot.

### Configuration
- A `smartcursor.json` file is created in `VintagestoryData/ModConfig`
- Behavior such as continuous updating and tool priorities can be customized

### Current status
This mod is experimental.
The current state is **"it works for me"** — use at your own risk.

# Todo
[ ] support Bucket and bowl for liquid

# Build & run
Why scons the donet echo system looks like really windows specfic
    .1 i don't have windows
    .2 i don't know how the mod building works, i simply copy paste the example from https://github.com/anegostudios/vsmodtemplate

So it's built like this:

    # This need to be ran only once
    scons VINTAGE_STORY=$(realpath <path to vs>) VINTAGE_STORY_DATA=$(realpath <your vs data location>)

    # default value for VINTAGE_STORY_DATA is ~/.config/VintagestoryData/ so you may not need this

    # than
    scons install run
    scons # or simply scons for build only


# Analyze
Don't forget to install roslynator first

    dotnet tool install -g roslynator.dotnet.cli
    scons analyze
