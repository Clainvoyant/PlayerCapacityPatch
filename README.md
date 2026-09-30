# PlayerCapacityPatch

A BepInEx + Harmony patch for ASKA that raises and stabilizes the effective multiplayer player capacity in the game runtime.

## What this mod does

This project patches several internal ASKA systems that assume a lower player count than the game is being configured to use:

- `NetworkRunner.SetupNetworkProjectConfig`
- Steam lobby max players
- Photon room max players
- `PlayerManager` backing arrays
- streaming payload arrays
- network color fallback logic
- spawn-race safety around `NetworkLink`

The goal is to keep Fusion, lobby/session capacity, player records, streaming state, and visual player colors consistent when more players are present than the base game expects.

## Project structure

- `Plugin.cs` – BepInEx plugin entry point and Harmony patch installation
- `PlayerLimits.cs` – central player capacity constants and detection logic
- `FusionConfigPatch.cs` – adjusts Fusion default player count
- `SteamLobbyPatch.cs` – adjusts Steam lobby max player count
- `PhotonRoomPatch.cs` – adjusts Photon room max player count
- `PlayerManagerCapacityPatch.cs` – expands player array capacity safely
- `NetworkStreamingCapacityPatch.cs` – expands streaming payload capacity and initializes new slots
- `NetworkLinkSpawnGuardPatch.cs` – defers spawn-race exceptions instead of crashing
- `PlayerNetworkColourFixPatch.cs` – restores colors for extra players when ASKA returns invalid/default values

## Build

This project targets .NET 6 and references ASKA/BepInEx assemblies from the local install path defined in the project file.

```bash
dotnet build -c Release
```

## Install

Build the mod and copy the generated DLL into the ASKA BepInEx plugin folder for your install.

Typical workflow:

1. Build the project
2. Copy the resulting DLL from `bin/Debug/net6.0/PlayerCapacityPatch.dll`
3. Place it in your ASKA `BepInEx/plugins` directory
4. Launch ASKA

## Notes

This project is experimental and intentionally patches private runtime state. It is designed for ASKA compatibility work and runtime diagnostics while testing higher-capacity multiplayer behavior.

## Extra player waypoint colors

ASKA's native waypoint initialization reads colors through `ColorTableConfig.TryGetColor`, which falls back to white when a player color ID is outside the four-entry `PlayerNetworkColors` table. The patch supplies the extra palette for actual player color IDs 4 through 31 in that lookup, so extra players' map icons use their defined palette color instead of white. Normal IDs and the local player's identity are not rewritten; single-player IDs 0 through 3 continue through ASKA's original lookup.

Each extra slot can be overridden in the BepInEx config under `[ExtraPlayerColors]` (`Player5` through `Player32`, corresponding to IDs 4 through 31). Values accept `#RRGGBB`, `#RRGGBBAA`, or normalized comma-separated channels such as `1.0,0.2,0.65,1.0`. Defaults preserve the palette. Color IDs are networked, but RGB values are resolved locally, so different client configs will not break the session; they can make clients see different colors for the same player.

## Player count

Set `Player Capacity.MaxPlayers` at the top of the BepInEx config to choose the multiplayer human-player limit. It defaults to `16` and accepts values from `4` to `32`. Change it before creating the lobby/session and use the same value on all clients; changing it does not resize an already-running session. The configurable extra palette covers the full supported range of IDs 4 through 31.
