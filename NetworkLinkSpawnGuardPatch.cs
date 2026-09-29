using System;
using HarmonyLib;
using SSSGame.Network;

namespace PlayerCapacityPatch;

internal static class NetworkLinkSpawnGuard
{
    internal static bool IsSpawnRace(
        Exception exception
    )
    {
        return exception is InvalidOperationException &&
               exception.Message.Contains(
                   "Networked properties can only be accessed when Spawned() has been called"
               );
    }
}


[HarmonyPatch(
    typeof(NetworkLink),
    nameof(NetworkLink._TryGetNext)
)]
internal static class NetworkLinkTryGetNextPatch
{
    [HarmonyFinalizer]
    private static Exception Finalizer(
        Exception __exception,
        ref bool __result,
        ref NetworkLink next
    )
    {
        if (__exception == null)
        {
            return null;
        }

        if (!NetworkLinkSpawnGuard.IsSpawnRace(__exception))
        {
            return __exception;
        }

        next = null;
        __result = false;

        Plugin.Log.LogWarning(
            "NetworkLink._TryGetNext deferred: " +
            "linked object has not completed Spawned() yet."
        );

        return null;
    }
}


[HarmonyPatch(
    typeof(NetworkLink),
    nameof(NetworkLink._TryGetPrevious)
)]
internal static class NetworkLinkTryGetPreviousPatch
{
    [HarmonyFinalizer]
    private static Exception Finalizer(
        Exception __exception,
        ref bool __result,
        ref NetworkLink previous
    )
    {
        if (__exception == null)
        {
            return null;
        }

        if (!NetworkLinkSpawnGuard.IsSpawnRace(__exception))
        {
            return __exception;
        }

        previous = null;
        __result = false;

        Plugin.Log.LogWarning(
            "NetworkLink._TryGetPrevious deferred: " +
            "linked object has not completed Spawned() yet."
        );

        return null;
    }
}