# GrassBag

GrassBag is a Hollow Knight mod that adds a status bar to the top right of your screen to shows you how much grass you've mowed. Team Cherry have confirmed that mowing all the grass in Hollow Knight is the only way to get the true ending in Silksong, so get mowing!

## Status Text

You'll see four numbers appear on the top right of your screen. One pair of numbers for your "global" progress and one pair for your "in room" progress.

For example, you might see: **32/88 globally -- 11/13 in room**. In this example: you've mowed **32** grass out of the **88** grass you've ever been in the same room (in this save file); and you've mowed **11** grass out of the **13** grass in the room/level/scene you're currently in.

Note that you only get credit for mowing a piece of grass once. You can't mow it, let it regrow, and then come back and mow it again for more points. You gotta find greener pastures.

## Grassward Compass

You'll also see a small green box that hovers around your knight. It will always point at the nearest grass that you haven't mowed yet (unless you've mowed all the grass in the room you're in of course!).

## Uncuttable Grass

Some of the grass in the game (especially in White Palace) seems to be bugged. You can only cut it _sometimes_. If you encounter such a tricky grass, just strike it with your nail (rather than c-dash or any other mowing method). It won't be cut in-game, but GrassBag will count it as mowed: it knows you tried your best.

## AllGrass.txt: Seeding the Global Total

The "global" total only increases when you enter a room with grass in it. The mod isn't aware of all the grass in the game right off the bat. But it can be!

If you visit every room with grass in it with this mod, then when you save your game an `AllGrass.txt` file will be created/overwritten in your save game directory (ex: mine is in `C:\Users\John\AppData\LocalLow\Team Cherry\Hollow Knight`). This file will have a line for every unique piece of grass you've found.

When you create a new game, the mod will check this file and seed the "global" total with all the grass in this `AllGrass.txt` file. The mod only reads from this file when you create a new game, so you can delete/muck-with this file without messing up your existing saves.

## Installing

First install the Modding API and the ModCommon mod. You can install both of these with the Hollow Knight Mod Installer.

Then grab the latest release of GrassBag from https://github.com/itsjohncs/GrassBag/releases (it'll be a zip file with this README in it as well as a `GrassBag.dll` file). Copy the `GrassBag.dll` file inside it into your `Mods/` directory with your other mods (ex: on my computer the directory is at `C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods`).

You've installed GrassBag! Enjoy being the lawnmower you always wanted to be.

## Issues

If you find problems, open up an issue in https://github.com/itsjohncs/GrassBag/issues or reach out to me on Discord.

Note: I've only created releases for Windows. I could make them for Mac as well. Open an issue in the GitHub repo if you're interested in that: https://github.com/itsjohncs/GrassBag.
