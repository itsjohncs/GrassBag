# GrassBag

-- This mod depends on the ModCommon mod, you'll have to install that as well --

This mod will search for destructible grass in each room you go into and keep a tally of how many you've cut. Useful for a Grass % run.

Your progress will be saved with your save file as you'd expect.

Separately, every time you save, an "AllGrass.txt" file is writted into your save game directory (ex: "C:\Users\John\AppData\LocalLow\Team Cherry\Hollow Knight"). This file contains the ID of every piece of grass you've seen (in that last save file). When you start a new game, all the IDs in "AllGrass.txt" are added into your save file immediately. So you could, in theory, start the game with an accurate count of all the grass in the game for you to cut.

Potential problems:

1. The grass hunting routine runs every 10 seconds. If it ends up causing lag I can speed it up significantly with a bit of work though.
2. How I've defined grass is very basic (if it's hittable and has "grass" in its name, its grass) so I don't know if that'll need to be tweaked.