# CS2-FixRandomSpawn
Fixes ConVar `mp_randomspawn` for any game mode

> [!CAUTION]
> The **`without-gamedata`** version does not support  setting ConVar **`mp_randomspawn_los`** and **`mp_randomspawn_dist`**.

## Requirments
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/)

## Installation
- Download the newest release from [Releases](https://github.com/qstage/CS2-FixRandomSpawn/releases)
- Move the /gamedata folder to a folder /counterstrikesharp
- Make a folder in /plugins named /FixRandomSpawn.
- Put the plugin files in to the new folder.
- Restart your server.

## Configuration
`css_randomspawn_reload` - Reload configuration
```json
{
    "warmup_mode": {
        "enable": false, // Enable the plugin only for warmup time
        "buy_anywhere": false, // Allow purchase anywhere on the map
        "alert_for_players": false // Notification for players about the enabled random spawns
    },
    "ConfigVersion": 1
}
```