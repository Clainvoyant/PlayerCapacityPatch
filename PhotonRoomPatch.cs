using Fusion.Photon.Realtime;
using HarmonyLib;

namespace PlayerCapacityPatch;

[HarmonyPatch(
    typeof(FusionRelayClient),
    nameof(FusionRelayClient.BuildEnterRoomParams)
)]
internal static class PhotonRoomPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref int __2)
    {
        int original = __2;

        __2 = PlayerLimits.NetworkCapacity;

        Plugin.Log.LogInfo(
            $"Photon BuildEnterRoomParams maxPlayers: " +
            $"{original} -> {__2}"
        );
    }
}