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
`environment_web_socket` | ws://localhost:8000 | Websocket url
`debug_mode` | false | some actions will be handled differently when `true`. Enables god mode toggle with KeyCode.LeftBracket
`use_custom_rules` | true | if an adventure will use the custom difficulty settings from `NeuroFTKCustomHouseRules.json` located in the same folder as the plugin dll. [rule info](src/GameConfigs/README.md)
`force_first_adventure` | false | only let neuro choose the first adventure map For the King (other maps not tested yet)
`is_multiplayer` | false | disables normal main menu actions
`always_resume` | true | disables new game if there is a previous save to load

> [!CAUTION]
> Multiplayer has NOT been tested & will likely break things

## Current State

- [x] Main menu
  - [x] Lore store
  - [x] Adventure setup
- [ ] Overworld flow
  - [ ] movement
    - [x] Land
    - [ ] Sea
    - [ ] Air
  - [ ] change equipment
  - [x] use items
- [ ] Encounters
  - [x] towns
    - [x] services
    - [x] market
      - [x] buying
      - [ ] selling
    - [x] quests
  - [ ] other
- [x] Combat
  - [x] enter combat encounter
  - [x] choose attack & target
  - [x] use items
  - [ ] change weapon
