# Neuro For the King integration
This mod allows [Neuro-sama](https://www.twitch.tv/vedal987) to play [For the King](https://store.steampowered.com/app/527230/For_The_King/)  
The mod uses a [Modified SDK](https://github.com/Pyran99/neuro-sdk-net35) due to the older .Net version used by the game

<img src="assets\FTK banner.jpg" width="500" style="vertical-align:middle;"> 

## Installation
### Normal install
Download the release file and add them to your For the king BepInEx plugins folder.

### Build project
**Required** game libraries not distributed here
- Assembly-CSharp.dll
- Assembly-CSharp-firstpass.dll
- UnityEngine.UI.dll
- PlayMaker.dll
> [!IMPORTANT]
> if you try to build this project yourself, I have the csproj setup to build directly to a custom file path. You must create a _TEXT_ file in the base folder called `output_path` with a file path to the folder you want the build to go to.  
> example: (your pc stuff)\ForTheKing\BepInEx\plugins\NeuroFTK

## Config data
See [Config Readme](src/GameConfigs/README.md)

## Known issues  
- Lost civ dlc is not supported. It may work but is intentionally left out of adventure choices
- The games pathfinding has a limited distance. This makes progressing some maps difficult. Multiplayer with a person can fix this

### Todo  
- [ ] make auto travel work for quests that are to far for games pathfinding  

### Undecided actions  
- market selling: equipment can be destroyed in battle, leaving this out would likely be more helpful
- Combat fleeing: fleeing is currently set to only be an option if that character is below 35% health. The action description recommends only using in emergencies, but it could still be used to often at times that result in a worse outcome

## Multiplayer  
- the mod will automatically claim any unclaimed player slots (up to config multiplayer_slots_taken).
- this mod cannot create an online game or select local co-op if single player mode is available for a map
- joining an online game is handled by setting is_multiplayer to true, which disables main menu actions (or just stop responses to the sdk), then manually joining a game.
- I do not know how or if direct friend invites work. The game uses a server list with name & password
- movement navigation can be assisted with another player (of the human variety) placing pings (alt+click), which the mod can move towards. Max game pathfind distance still applies. example use: bring neuro to a shoreline to get picked up by a boat.




