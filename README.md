# Neuro For the King integration
This mod allows [Neuro-sama](https://www.twitch.tv/vedal987) to play [For the King](https://store.steampowered.com/app/527230/For_The_King/)  
The mod uses a [Modified SDK](https://github.com/Pyran99/neuro-sdk-net35) due to the older .Net version used by the game

<img src="assets\FTK banner.jpg" width="500" style="vertical-align:middle;"> 

## Game Libraries
not distributed here
- Assembly-CSharp.dll
- Assembly-CSharp-firstpass.dll
- UnityEngine.UI.dll
- PlayMaker.dll
> [!IMPORTANT]
> if you try to build this project yourself, I have the csproj setup to build directly to a custom file path. You must create a _TEXT_ file in the base folder called `output_path` with a file path to the folder you want the build to go to.  
> example: (your pc stuff)\ForTheKing\BepInEx\plugins\NeuroFTK

## Config data
Config files are automatically generated in the same folder as the dll
Name | Default | Description
--- | --- | ---
environment_web_socket | ws://localhost:8000 | Websocket url
allow_cheats | false | ok cheater
debug_mode | false | some actions may be handled differently when `true`
use_custom_rules | true | if an adventure will use the custom difficulty settings from `NeuroFTKCustomHouseRules.json` located in the same folder as the plugin dll. [rule info](src/GameConfigs/README.md)
is_multiplayer | false | disables normal main menu actions. *multiplayer NOT implemented as of v0.9.0*
launch_resume | true | disables new game if there is a previous save to load. Only used for the initial game startup
max_hex_search | 100 | the max amount of hexes to send for context & choice list of actions that require picking a hex (Airship movement can be 168+). For movement the removed hexes start from the furthest, for items that pick a hex it is based on the order the map was created (aka nobody knows)
force_custom_adventure | false | force new games to only allow the specified adventure from `custom_adventure_code`. This **OVERRIDES** launch_resume & only allows new games
custom_adventure_code | ftk | the [Config code](#adventure-details) for the forced adventure. Only used if `force_custom_adventure` is true

> [!CAUTION]
> Multiplayer has NOT been tested & *will* break things

## Adventure details
Adventure | Config code | Description | Integration State
--- | --- | --- | ---
For the King | ftk | main adventure with travelling by land, sea, air | working
Frost Adventure | fa | similar to FTK, no air travel, damage taken at end of each turn | working
Into the Deep | id | primarily sea travel | not fully tested
Dungeon Crawl | dc | search for dungeons around map, mostly land travel, some boating | working
Hildebrant's Cellar | hc | dungeon run only | working
Gold Rush | gr | multiplayer only | untested  

hildebrants cellar is a dungeon runner only (no movement decisions)  
other adventures use hex movement


## Current State  

v0.9.0: Tony was let loose for a long time with no game breaking issues. praise be the rng gods  

### Todo  

- [ ] Overworld flow
  - [ ] change equipment (empty slots only)
- [ ] Combat
  - [ ] change weapon (currently only when unarmed)
  - [ ] use focus
- [ ] encounters use focus
- [ ] make auto travel work for quests that are to far for games pathfinding

### Undecided actions

- market selling: equipment can be destroyed in battle, leaving this out would likely be more helpful


