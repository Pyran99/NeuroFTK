# Neuro For the King integration
This mod allows [Neuro-sama](https://www.twitch.tv/vedal987) to play [For the King](https://store.steampowered.com/app/527230/For_The_King/)  
The mod uses a [Modified SDK](https://github.com/Pyran99/neuro-sdk-net35) due to the older .Net version used by the game

<img src="assets\FTK banner.jpg" width="500" style="vertical-align:middle;"> 

## Game Libraries
- Assembly-CSharp.dll
- Assembly-CSharp-firstpass.dll
- UnityEngine.UI.dll
- PlayMaker.dll

## Config data
Config files are automatically generated in the same folder as the dll
Name | Default | Description
--- | --- | ---
`environment_web_socket` | ws://localhost:8000 | Websocket url
`debug_mode` | false | some actions will be handled differently when `true`. Enables god mode toggle with KeyCode.LeftBracket
`use_custom_rules` | true | if an adventure will use the custom difficulty settings from `NeuroFTKCustomHouseRules.json` located in the same folder as the plugin dll. 
`force_first_adventure` | false | only let neuro choose the first adventure map For the King

> [!CAUTION]
> Co-op has NOT been tested & will likely break things

## Current State
> [!NOTE]
> Some things may be in a working state but code is commented out/altered in a way to facilitate testing

Main menu actions for purchasing lore unlocks, pressing resume, or pressing new game
