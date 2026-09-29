using System.Reflection;
using Fusion;
using HarmonyLib;

namespace PlayerCapacityPatch;

[HarmonyPatch]
internal static class FusionConfigPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        var method = AccessTools.Method(
            typeof(NetworkRunner),
            nameof(NetworkRunner.SetupNetworkProjectConfig),
            new[] { typeof(NetworkRunnerInitializeArgs) }
        );

        Plugin.Log.LogInfo(
            $"SetupNetworkProjectConfig target resolved: {method}"
        );

        return method;
    }

    [HarmonyPostfix]
    private static void Postfix(
        NetworkProjectConfig __result
    )
    {
        if (
            __result == null ||
            __result.Simulation == null
        )
        {
            return;
        }

        int original =
            __result.Simulation.DefaultPlayers;

        // Singleplayer uses a one-player Fusion simulation.
        // Do not expand it, otherwise Fusion assigns the local
        // player a high PlayerRef such as Player:15.
        if (
            !PlayerLimits.IsDedicatedServer &&
            original == 1
        )
        {
            Plugin.Log.LogInfo(
                $"Fusion DefaultPlayers preserved: {original} " +
                $"(singleplayer)"
            );

            return;
        }

        __result.Simulation.DefaultPlayers =
            PlayerLimits.NetworkCapacity;

        Plugin.Log.LogInfo(
            $"Fusion DefaultPlayers: " +
            $"{original} -> " +
            $"{__result.Simulation.DefaultPlayers}"
        );
    }
}