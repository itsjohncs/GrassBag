# GrassBag

Hollow Knight mod that adds a status bar to the top right of your screen that shows you how much grass you've mowed!

It shows you 2 pairs of numbers. A "global" pair showing you how much you've mowed in that save file out of all the grass you've been near. And an "in-room" pair showing you how much you've mowed in the room that you're in (useful for finding grass you've missed... gotta mow them all!).

You don't get credit for leaving a room, coming back, and mowing the same grass again. You gotta find greener pastures to mow.

## Seeding the Global Total

The "global total" only increases when you actually enter a room with grass in it. The mod isn't aware of all the grass in the game right off the bat. But it can be!

If you visit every room with grass in it with this mod, then when you save your game an "AllGrass.txt" file will be created/overwritten in your save game directory (ex: "C:\Users\John\AppData\LocalLow\Team Cherry\Hollow Knight"). This file will have a line for every unique piece of grass you've found.

Then when you create a new game, your new save's "global total" will be seeded with this "AllGrass.txt" file.

## Installing

First install the Modding API and the ModCommon mod. You can install both of these with the Hollow Knight Mod Installer.

Then grab the latest release from https://github.com/itsjohncs/GrassBag/releases (it'll be a zip file) then copy the GrassBag.dll file inside it into your Mods/ directory with your other mods (ex: on my computer the directory is at C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods).

You're done! Enjoy being the lawnmower you always wanted to be.

Note: I've only created releases for Windows. I could make them for Mac as well. Open an issue in the GitHub repo if you're interested in that: https://github.com/itsjohncs/GrassBag.