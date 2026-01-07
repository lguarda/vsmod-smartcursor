![No shame it's chatgpt](logo.png)

# SmartCursor Vs mod
This is a Vintagestory client side mode which aim to implement the smart cursor feature from Terraria (In Terraria it's actually named smart cursor)

# How it Works
When the key bind is pressed the current selected block is analyzed to determine the right tool (axe, pickaxe, knife, shovel)
Than first the hot bar is scanned to find the first proper tool that match, than fallback to inventory.
Finally it will swap the position of the tool with your current active toolbar slot item.

Once key bind is release, or pressed again if toggle key bind was used, then the item will be swapped back with the tool.
Right now the keybind is <r> and <`> for the toggle mode

The current state of this mod is "it work for me" so be careful.

# Todo
[X] support corpse toggle knife
[ ] support Bucket and bowl for liquid
[X] implement a continuous mod so tool is constantly swapped against targeted block without having to cycle the hotkey
[X] support configurable tools selection (maybe for other mods integration)

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

![Some action](demo/vsmod-smartcursor-demo.gif)
![Some action](demo/vsmod-smartcursor-demo-inventory-swap.gif)
