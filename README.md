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
dotnet build PlayerCapacityPatch.csproj -nologo
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
