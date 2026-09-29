using System;
using HarmonyLib;
using SandSailorStudio.Platform;

namespace PlayerCapacityPatch;

[HarmonyPatch(
    typeof(SteamUsers),
    nameof(SteamUsers.CreateLobby),
    new Type[]
    {
        typeof(PlatformUsers.LobbyPrivacyType),
        typeof(int)
    }
)]
internal static class SteamCreateLobbyPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref int __1)
    {
        int original = __1;

        __1 = PlayerLimits.NetworkCapacity;

        Plugin.Log.LogInfo(
            $"Steam CreateLobby maxPlayers: {original} -> {__1}"
        );
    }
}