using Fusion;
using HarmonyLib;
using SSSGame;
using SSSGame.Network;
using UnityEngine;

namespace PlayerCapacityPatch;

[HarmonyPatch(
    typeof(PlayerManager),
    nameof(PlayerManager.OnPlayerJoined),
    new[]
    {
        typeof(NetworkRunner),
        typeof(PlayerRef)
    }
)]
internal static class NetworkStreamingStateDiagnosticPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PlayerRef player
    )
    {
        var worldDataManager =
            Object.FindObjectOfType<NetworkWorldDataManager>();

        if (worldDataManager == null)
        {
            Plugin.Log.LogWarning(
                $"STREAM DIAG: " +
                $"NetworkWorldDataManager not found " +
                $"after join {player}"
            );

            return;
        }

        var streamingManager =
            worldDataManager._networkStreamingManager;

        if (streamingManager == null)
        {
            Plugin.Log.LogWarning(
                $"STREAM DIAG: " +
                $"NetworkStreamingManager is null " +
                $"after join {player}"
            );

            return;
        }

        int compressedLength =
            streamingManager._compressedPayloads?.Length ?? -1;

        int currentSizesLength =
            streamingManager._currentPayloadSizes?.Length ?? -1;

        int uncompressedCount =
            streamingManager._uncompressedPayloads?.Count ?? -1;

        int subscribersLength =
            streamingManager._subscribers?.Length ?? -1;

        Plugin.Log.LogWarning(
            $"STREAM DIAG: " +
            $"player={player}, " +
            $"compressed={compressedLength}, " +
            $"currentSizes={currentSizesLength}, " +
            $"uncompressedCount={uncompressedCount}, " +
            $"subscribers={subscribersLength}"
        );
    }
}