# ProspectTogether

This is a fork of the awesome ProspectorInfo mod from P3t3rix (https://github.com/p3t3rix-vsmods/VsProspectorInfo).
ProspectTogether aims to make it easy to store, analyse and share your prospecting data.
You can also use this mod in single player.
The only downside compared to ProspectorInfo is that this mod also needs to be installed on the server.

## Quick Guide
* Drop the zip file into your `Mods` folder or use 1-click install from VSModDB.
* Just keep prospecting, the mod will automatically record any data found while the pick is used in Density Search mode.
* The data of each prospected chunk is shown on the map when you hold the pick in your hand or when the "Show Overlay" option is enabled.
* Use `Ctrl + P` to quickly show/hide the settings dialog.
* Use the heatmap setting and select an ore to see where certain ores are more likely to occur.
* Configure autosharing if you want to share your data with the whole server or a certain group.


## Migration from ProspectorInfo

This mod will automatically import existing prospecting data from the ProspectorInfo mod.
To be more specific, in the `%Vintage_Story_Data%/ModData/YourWorldId/` directory, it will copy the file `vsprospectorinfo.data.json` to `prospectTogetherClient.json`.
Note that there have been cases, where ProspectorInfo was unable to parse the output of the Prospecting Pick.
These entries cannot be imported. So you might have to prospect some chunks again.

## Data Sharing
If you want to share your existing prospecting data with other players on the server, use `.pt sendall` or the "Send All Now" on the map dialog (use `Ctrl + P` to open it).
This will send all your prospecting data to the server and all other players. You usually only have to do this once.
The server stores this information in `%Vintage_Story_Data%/ModData/YourWorldId/prospectTogetherServer.json`.
If you also want to share newly added prospecting data in the future, enable autosharing. 
You can do this either via the dialog on the map (use `Ctrl + P` to open it) or by using `.pt autoshare true`.

If you play on a PvP server, you may only want to share your data with a certain group of players.
Use `Ctrl + P` to open the settings. In the dialog, select the group that you want to share your data with.
You can use the "Send All Now" button, to send all your prospecting data to the selected group.

When autosharing is enabled, you will send all future prospecting data to the configured group.
Data sent to "All players" will be received by all players, regardless of their configured group.
Data sent to the configured group, will only be received by players in that group.
If autosharing is disabled, you will neither send nor receive any prospecting data.



## Client Commands

    .pt - main command for the mod and the default sub-command is to 'showoverlay'
    .pt showoverlay [true,false] - Show or hide the overlay on the map. Toggles without argument.
    .pt showborder [true,false] - Show or hide the border around chunks. Toggles without argument.
    .pt showgui [true,false] - Show the GUI where you can configure the mode (default or heatmap) and select the ore that should be heatmapped.
    .pt setcolor (overlay|border|zeroheat|lowheat|highheat) [0-255] [0-255] [0-255] [0-255] - Sets the color of the respective element.
    .pt setborderthickness [1-5] - Sets the border thickness. 
    .pt mode [0-1] - Sets the map mode. Supported modes: 0 (Default) and 1 (Heatmap)
    .pt heatmapore [oreName] - Changes the heatmap mode to display a specific ore.
        No argument resets the heatmap back to all ores. Can only handle the ore name in your selected language or the ore tag.
        Examples: game:ore-emerald, game:ore-bituminouscoal, Cassiterite.
    .pt autoshare [true,false] - Set automatic sharing of prospecting data.
    .pt setsaveintervalminutes [1-60] - Periodically store the prospecting data (on the client) every x minutes.
    .pt sendall - Send all your existing prospecting information to the server. You usually only have to run this command once per server.

### Client Configuration

    TextureColor [0-255] [0-255] [0-255] [0-255] - The default color to use for the overlay. Default: 150 125 150 128
    BorderColor [0-255] [0-255] [0-255] [0-255] - The default color to use for the border. Default: 0 0 0 200
    ZeroHeatColor [0-255] [0-255] [0-255] [0-255] - Heatmap color for zero relative density. Default: 0 0 0 0
    LowHeatColor [0-255] [0-255] [0-255] [0-255] - Heatmap color for low relative density. Default: 85 85 181 128
    HighHeatColor [0-255] [0-255] [0-255] [0-255] - Heatmap color for low relative density. Default: 168 34 36 128
    BorderThickness [1-5] - The thickness, in pixels, of the border color. Default: 1
    RenderBorder [true,false] - Whether or not to render the border at all. Default: true
    AutoToggle [true,false] - Whether or not to toggle the overlay on the map automatically, 
                              based on the player equipping/unequipping a prospecting pick. Default: true
    HeatMapOre [oreName] - The ore selected for the heatmap.
    MapMode [0-1] - The mode of the map.
    SaveIntervalMinutes [1-60] - Periodically store the prospecting data every x minutes. Default: 5
    AutoShare [true,false] - Share prospecting data with configured players on the server. Default: false
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
Clone the repository with submodules included: "git clone --recursive"
To compile the mod you also need to set 2 environment variables:
- VINTAGE_STORY => the path to the game directory e.g. c:\games\vintagestory
- VINTAGE_STORY_DATA => the path to the games data directory typically located somewhere in appdata e.g. C:\Users\MyUser\AppData\Roaming\VintagestoryData

## Create a release
To create a release just compile the solution in Release configuration. A folder named "release" should appear in the solution directory.
This can then be zipped to be uploaded to the mod-db.
