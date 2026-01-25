![No shame it's chatgpt](logo.png)

# SmartCursor Vs mod
This is a Vintagestory client side mode which aim to implement the smart cursor feature from Terraria (In Terraria it's actually named smart cursor)

# SmartCursorPlus
I didn't find a way to place block from client side, so SmartCursorPlus is not only client side it's also server side
It's goal is the implement kind of the same behavior as the smart placement in Terraria, but a little bit different since block placement is not absolute in VS like it is in Terraria
In VS we need, to target a block face then the block we place will be placed next to the face we pointed, which is really annoying for stairs and roof for example, look at the gif it show better how it work.

# How it Works
SmartCursor automatically selects the most appropriate tool based on what you are looking at.

When the keybind is pressed, the mod analyzes:
- the targeted entity (if any currently only support dead corps to pop the knife)
- otherwise the targeted block

Based on this analysis, it determines the preferred tool
(axe, pickaxe, hammer, knife, shovel, scythe, shears, etc.).

![wa](demo/vsmod-smartcursor-demo.gif)
![zaa](demo/vsmod-smartcursor-continuous-demo.gif)
![aaaa](demo/vsmod-smartcursor-demo-inventory-swap.gif)
![carendouf](demo/vsmod-smartcursor-placement-demo.gif)

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
- **Placement**: (default 'f') **Only with SmartCursorPlus**
  Press once to select you will see where block will be placed when you click, when the pich is above the horizon (kind of), it trigger the stair placement mod

### Restore behavior
- When SmartCursor deactivates (key released or toggle off), the original item is restored to its original slot.

### Blacklist
- You can black list item in the current active hotbar slot by pressing a key (default: <ctrl+alt+R>) it will toggle on and off black list for this item
  it can be useful for quest item ex: "Tin bronze pickaxe" if you want to be sure the mod will not pop this item.
  It can also be used if the mod has a bug or if other mod as some issue, for example the mod walkingstick the item property is tagged as pickaxe, so the mod can pop them, so you can simply black list it.

### Configuration
- A `smartcursor.json` file is created in `VintagestoryData/ModConfig`
- Behavior such as continuous updating and tool priorities can be customized


### Current status
This mod is experimental.
The current state is **"it works for me"** — use at your own risk.

# Todo
- Support block rotation for smart placement
- Support placement block downward
- Add other type of swap (ex: like for the worked clay which spawn)

# Build & run
Why scons? the dotnet echo system looks like really windows specific
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
