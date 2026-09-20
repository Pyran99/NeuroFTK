## Config data  
> Config files are automatically generated in the same folder as the dll  

Name | Default | Description
--- | --- | ---
environment_web_socket | ws://localhost:8000 | Websocket url
allow_cheats | false | ok cheater
debug_mode | false | some actions may be handled differently when `true`
use_custom_rules | true | if an adventure will use the custom difficulty settings from `NeuroFTKCustomHouseRules.json` located in the same folder as the plugin dll. [rule info](#map-rules)
is_multiplayer | false | disables normal main menu actions. [Multiplayer details](README.md#multiplayer)
multiplayer_slots_taken | 3 | the max number of characters in a co-op game this mod will automatically take
launch_resume | true | disables new game if there is a previous save to load. Only used for the initial game startup
max_hex_search | 50 | the max amount of hexes to send for context & choice list of actions that require picking a hex (late-game Airship movement can be 168+). The removed hexes are chosen at random (only empty hexes)
force_custom_adventure | false | force new games to only allow the specified adventure from `custom_adventure_code`. This **OVERRIDES** launch_resume & only allows new games
custom_adventure_code | ftk | the [Config code](#adventure-details) for the forced adventure. Only used if `force_custom_adventure` is true

## Adventure details  
> *Map navigation is limited for the large maps. [Map navigate support](README.md#multiplayer) is possible in multiplayer

Adventure | Config code | Description | Integration State
--- | --- | --- | ---
For the King | ftk | main adventure with travelling by land, sea, air | working*
Frost Adventure | fa | similar to FTK, no air travel, damage taken at end of each turn | working*
Into the Deep | id | primarily sea travel | working*
Dungeon Crawl | dc | search for dungeons around map, mostly land travel, some boating | working*
Hildebrant's Cellar | hc | dungeon run only | working
Gold Rush | gr | multiplayer only (local or online). small, land only map | working  



## Map Rules  
> [!CAUTION]
> Values are not clamped & may break things if outside Range  

Rule | Range | Details
--- | --- | ---
Chaos frequency | 3-25 | higher = easier
Life pool | 0-9 | higher = easier
Economy inflation | 30-150 | lower = easier
Gold target | 25-1500 | lower = easier

Frost adventure's chaos value is locked 0  
HildebrantsCellar can't be customized  
Grave robber is co-op only, Chaos & Life is locked  
Lost civ is the dlc, inflation locked 80

### Defaults

Adventure | Chaos | Life | Economy | Gold
--- | --- | --- | --- | ---
KillVexor (for the king) | 8 | 6 | 80 | 0
FrostAdventure | 0 | 6 | 80 | 0
Pirates (into the deep) | 18 | 6 | 80 | 0
DungeonCrawl | 10 | 6 | 80 | 0
HildebrantsCellar | 8 | 6 | 80 | 0
GraveRobber (gold rush) | 0 | 0 | 30 | 100
LostCiv | 11 | 6 | 80 | 100

  