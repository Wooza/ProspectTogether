# ProspectTogether

This is a fork of the awesome ProspectorInfo mod from P3t3rix ([https://mods.vintagestory.at/show/mod/1235](https://mods.vintagestory.at/show/mod/1235)).
ProspectTogether aims to make it easy to store, analyse and share your prospecting data.
You can also use this mod in single player and client-side only.
If you want to share prospecting data, this mod must be installed on the server as well.

## Quick Guide
* Drop the zip file into your `Mods` folder or use 1-click install from VSModDB.
* Just keep prospecting, the mod will automatically record any data found while the pick is used in Density Search mode.
* The data of each prospected chunk is shown on the map, when the ProspectTogether map layer is enabled.
* Use the heatmap setting and select an ore to see where certain ores are more likely to occur.
* Configure autosharing if you want to share your data with the whole server or a certain group.

## Known issues
* This mod displays the most recent prospecting reading per chunk. This is inaccurate, [because the readings are position-based and not chunk-based](https://wiki.vintagestory.at/Prospecting_Pick#Notes), however, readings within a chunk are usually close enough to each other that this does not matter much.
* The prospecting data is only captured if the map or mini-map is used, i.e., if the dot from the vanilla prospecting appears, than the information is also stored in this mod.

## Troubleshooting
If you found a problem with the mod, feel free to report it. However, keep in mind that I'm maintaining this mod in my spare time, so don't expect me to immediately fix every bug.
Please keep the following in mind, when reporting an error:
- Describe the problem as precise as you can. This makes it a lot easier for me to reproduce and fix a bug. 
  Ideally, you give me a step-by-step explanation on how to reproduce the bug.
    - Bad:
        - "Mod does not work!"
        - "Mod crashes!"
    - Good:
        - "I just prospected a new chunk while playing in SinglePlayer. Then I tried clicking on xyz and nothing seems to happen. Game Version X.Y, Mod Version X.Y, OS: Windows/Linux. I found the following errors in the log file, which might be related: [Log file contents here]"
- Check the log files for suspicious entries
    - The game has several log files, though I'm not sure what ends up where, so make sure to skim over all of them.
      You can find them in `C:\Users\YourUserName\AppData\Roaming\VintagestoryData\Logs`. 
    - Note that the game creates new log files on each game start and archives the old entries 
      so make sure you open the correct log files.
    - It should be sufficient to use Ctrl+F to search for `ProspectTogether`.
- Ideally you create an Issue on [Github](https://github.com/Wooza/ProspectTogether/issues).
    - This requires a free account, but makes it easier for me to track different problems/questions, etc. instead having to follow individual comments on the Mod page.
    - Posting on the Mod page is ok, but I might be slower to react to that.

## Changelog
See [Changelog](CHANGELOG.md)

## Migration from ProspectorInfo

This mod will automatically import existing prospecting data from the ProspectorInfo mod.
To be more specific, in the `%Vintage_Story_Data%/ModData/YourWorldId/` directory, it will copy the file `vsprospectorinfo.data.json` to `prospectTogetherClient.json`.
Note that there have been cases, where ProspectorInfo was unable to parse the output of the Prospecting Pick.
These entries cannot be imported. So you might have to prospect some chunks again.

## Data Sharing
If you want to share your existing prospecting data with other players on the server, use `.pt sendall` or the "Send All Now" on the map dialog.
This will send all your prospecting data to the server and all other players. You usually only have to do this once.
The server stores this information in `%Vintage_Story_Data%/ModData/YourWorldId/prospectTogetherServer.json`.
If you also want to share newly added prospecting data in the future, enable autosharing. 
You can do this either via the dialog on the map or by using `.pt autoshare true`.

If you play on a PvP server, you may only want to share your data with a certain group of players.
In the map dialog, select the group that you want to share your data with.
You can use the "Send All Now" button, to send all your prospecting data to the selected group.

When autosharing is enabled, you will send all future prospecting data to the configured group.
Data sent to "All players" will be received by all players, regardless of their configured group.
Data sent to the configured group, will only be received by players in that group.
If autosharing is disabled, you will neither send nor receive any prospecting data.


## Client Commands

    .pt showborder [true|false] - Show or hide the border around chunks. Toggles without argument.
    .pt setcolor (overlay|border|zeroheat|lowheat|highheat) [0-255] [0-255] [0-255] [0-255] - Sets the color of the respective element.
    .pt setborderthickness [1-5] - Sets the border thickness. 
    .pt mode [0-1] - Sets the map mode. Supported modes: 0 (Default) and 1 (Heatmap)
    .pt heatmapore [oreName] - Changes the heatmap mode to display a specific ore.
        No argument resets the heatmap back to all ores. Can only handle the ore name in your selected language or the ore tag.
        Examples: game:ore-emerald, game:ore-bituminouscoal, Cassiterite.
    .pt autoshare [true|false] - Set automatic sharing of prospecting data.
    .pt setsaveintervalminutes [1-60] - Periodically store the prospecting data (on the client) every x minutes.
    .pt sendall - Send all your existing prospecting information to the server. You usually only have to run this command once per server.

### Client Configuration

    TextureColor [0-255] [0-255] [0-255] [0-255] - The default color to use for the overlay. Default: 150 125 150 128
    BorderColor [0-255] [0-255] [0-255] [0-255] - The default color to use for the border. Default: 0 0 0 200
    ZeroHeatColor [0-255] [0-255] [0-255] [0-255] - Heatmap color for zero relative density. Default: 0 0 0 0
    LowHeatColor [0-255] [0-255] [0-255] [0-255] - Heatmap color for low relative density. Default: 85 85 181 128
    HighHeatColor [0-255] [0-255] [0-255] [0-255] - Heatmap color for low relative density. Default: 168 34 36 128
    BorderThickness [1-5] - The thickness, in pixels, of the border color. Default: 1
    RenderBorder [true|false] - Whether or not to render the border at all. Default: true
    HeatMapOre [oreName] - The ore selected for the heatmap.
    MapMode [0-1] - The mode of the map.
    SaveIntervalMinutes [1-60] - Periodically store the prospecting data every x minutes. Default: 5
    AutoShare [true|false] - Share prospecting data with configured players on the server. Default: false
    ShareGroupUid [int] - Group Uid to which your data is sent. Use -1 for all players. Default: -1

## Server Commands (requires admin role)

    /pt setsaveintervalminutes [1-60] - Periodically store the prospecting data (on the server) every x minutes.

### Server Configuration

    SaveIntervalMinutes [1-60] - Periodically store the prospecting data every x minutes. Default: 5

## Usage

Whenever you finish prospecting a chunk, the data is saved into the ModData folder and added to the chunk info of the world map. 

Additionally, if you enabled autosharing, the data is also sent to the server, where it is also stored and sent to all players.

The mod renders a transparent square of all chunks that have been prospected. If a chunk is re-prospected, the message is simply overwritten. The rendering of these squares can be toggled with the .pt command.

After prospecting, the info will be displayed in the tooltip of the minimap when hovering over the chunk. This info is stored in `%Vintage_Story_Data%/ModData/YourWorldId/prospectTogetherClient.json`.

On the server side, the info is stored in `%Vintage_Story_Data%/ModData/YourWorldId/prospectTogetherServer.json`.

![image](https://user-images.githubusercontent.com/5238284/79952656-09e3f680-847b-11ea-96c9-b4cb9b47355f.png)

### Heatmap

A map mode that displays the relative density of the ores on the map via a color gradient. Can be enabled/disabled and switched between displaying the density of just one ore and displaying the density of all ores (The highest density per chunk is picked).

Normal map (map mode 0)
![map](https://user-images.githubusercontent.com/24532072/168427928-96b134aa-288d-4d4c-ade6-ddcb002c6d51.png)


Heatmap (map mode 1)
![heatmap](https://user-images.githubusercontent.com/24532072/168427930-571788d3-eca5-4cbb-b6d6-caf2c6b9bcd1.png)


Heatmap for Cassiterite only (map mode 1; heatmapore Cassiterite)
![heatmapCassiterite](https://user-images.githubusercontent.com/24532072/168427932-9fd7020f-3248-4708-8f68-25a082a86bd2.png)



## Compiling
To compile the mod you also need to set 2 environment variables:
- `VINTAGE_STORY`: Path to the game
- `VINTAGE_STORY_DATA` Path to the game data

The following commands should work in cmd, unless you changed the installation directory:
- `setx VINTAGE_STORY %APPDATA%\Vintagestory`
- `setx VINTAGE_STORY %APPDATA%\VintagestoryData`

## Create a release
1. Update version in `modinfo.json` according to [SemVer](https://semver.org/)
2. (Optional) Update game version used for CI build in `ci.yaml`
3. Add Changelog entry for new version in `CHANGELOG.md`
4. Commit and push to main.
5. Wait for CI pipeline to produce zip File and download it
6. Create GitHub release for new version `vX.Y.Z` with changelog text and attach zip file
7. Create new release on `https://mods.vintagestory.at/prospecttogether` with changelog text and attach zip file
