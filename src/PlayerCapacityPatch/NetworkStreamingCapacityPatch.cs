using Fusion;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.IO;
using SSSGame;
using SSSGame.Network;
using UnityEngine;

namespace PlayerCapacityPatch;

internal static class NetworkStreamingCapacity
{
    // Vanilla:
    //     4 human players -> streaming arrays length 5
    //
    // Preserve that same +1 relationship.
    internal static int TargetSize =>
        PlayerLimits.MaxHumanPlayers + 1;

    internal static void EnsureCapacity(
        string source
    )
    {
        var worldDataManager =
            Object.FindObjectOfType<NetworkWorldDataManager>();

        if (worldDataManager == null)
        {
            Plugin.Log.LogWarning(
                $"Streaming capacity: " +
                $"NetworkWorldDataManager not found ({source})"
            );

            return;
        }

        var manager =
            worldDataManager._networkStreamingManager;

        if (manager == null)
        {
            Plugin.Log.LogWarning(
                $"Streaming capacity: " +
                $"NetworkStreamingManager not available ({source})"
            );

            return;
        }

        var oldCompressed =
            manager._compressedPayloads;

        var oldSizes =
            manager._currentPayloadSizes;

        int oldCompressedLength =
            oldCompressed?.Length ?? 0;

        int oldSizesLength =
            oldSizes?.Length ?? 0;

        int targetSize =
            TargetSize;

        if (
            oldCompressedLength >= targetSize &&
            oldSizesLength >= targetSize
        )
        {
            return;
        }

        // -------------------------------------------------
        // Expand compressed payload array
        // -------------------------------------------------

        var newCompressed =
            new Il2CppReferenceArray<MemoryStream>(
                targetSize
            );

        if (oldCompressed != null)
        {
            for (
                int i = 0;
                i < oldCompressed.Length &&
                i < newCompressed.Length;
                i++
            )
            {
                newCompressed[i] =
                    oldCompressed[i];
            }
        }

        // -------------------------------------------------
        // Expand payload-size array
        // -------------------------------------------------

        var newSizes =
            new Il2CppStructArray<int>(
                targetSize
            );

        if (oldSizes != null)
        {
            for (
                int i = 0;
                i < oldSizes.Length &&
                i < newSizes.Length;
                i++
            )
            {
                newSizes[i] =
                    oldSizes[i];
            }
        }

        // Both arrays must be replaced before asking ASKA
        // to initialise any new streaming slots.
        manager._compressedPayloads =
            newCompressed;

        manager._currentPayloadSizes =
            newSizes;

        // -------------------------------------------------
        // Initialise newly-added slots using ASKA's own
        // native initialisation routine.
        // -------------------------------------------------

        int firstNewIndex =
            oldCompressedLength;

        if (oldSizesLength < firstNewIndex)
        {
            firstNewIndex =
                oldSizesLength;
        }

        for (
            int i = firstNewIndex;
            i < targetSize;
            i++
        )
        {
            manager
                ._InitializeCompressedStreamingPayload(i);
        }

        Plugin.Log.LogInfo(
            $"Network streaming capacity expanded: " +
            $"compressed {oldCompressedLength} -> " +
            $"{manager._compressedPayloads.Length}, " +
            $"sizes {oldSizesLength} -> " +
            $"{manager._currentPayloadSizes.Length} " +
            $"({source})"
        );
    }
}


// Do NOT Harmony-patch NetworkStreamingManager directly.
//
// PlayerManager.OnPlayerJoined is a callback we have already
// established is safe to patch in this IL2CPP build.
[HarmonyPatch(
    typeof(PlayerManager),
    nameof(PlayerManager.OnPlayerJoined),
    new[]
    {
        typeof(NetworkRunner),
        typeof(PlayerRef)
    }
)]
internal static class NetworkStreamingCapacityPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PlayerRef player
    )
    {
        NetworkStreamingCapacity.EnsureCapacity(
            $"Player joined {player}"
        );
    }
}