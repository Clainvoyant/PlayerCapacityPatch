using Fusion;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SSSGame;

namespace PlayerCapacityPatch;

internal static class PlayerManagerCapacity
{
    internal static int PlayerArraySize =>
        PlayerLimits.PlayerManagerCapacity;

    internal static void EnsureCapacity(
        PlayerManager manager,
        string source
    )
    {
        var oldPlayers = manager._players;

        if (
            oldPlayers != null &&
            oldPlayers.Length >= PlayerArraySize
        )
        {
            return;
        }

        int oldLength = oldPlayers?.Length ?? 0;

        var newPlayers =
            new Il2CppReferenceArray<PlayerManager.Player>(
                PlayerArraySize
            );

        for (int i = 0; i < PlayerArraySize; i++)
        {
            if (
                oldPlayers != null &&
                i < oldPlayers.Length &&
                oldPlayers[i] != null
            )
            {
                // Preserve ASKA's already-initialized slots.
                newPlayers[i] = oldPlayers[i];
            }
            else
            {
                // ASKA expects each slot to contain a Player
                // object, even while the slot is unused.
                newPlayers[i] = new PlayerManager.Player();
            }
        }

        manager._players = newPlayers;

        Plugin.Log.LogInfo(
            $"PlayerManager._players resized: " +
            $"{oldLength} -> {newPlayers.Length} " +
            $"({source})"
        );
    }
}


// Resize whenever ASKA resets/recreates its player storage.
[HarmonyPatch(
    typeof(PlayerManager),
    "_ResetPlayers"
)]
internal static class PlayerManagerResetPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PlayerManager __instance
    )
    {
        PlayerManagerCapacity.EnsureCapacity(
            __instance,
            "_ResetPlayers"
        );
    }
}


// Safety net: guarantee sufficient storage before ASKA
// processes any player joining.
[HarmonyPatch(
    typeof(PlayerManager),
    nameof(PlayerManager.OnPlayerJoined),
    new[]
    {
        typeof(NetworkRunner),
        typeof(PlayerRef)
    }
)]
internal static class PlayerManagerJoinCapacityPatch
{
    [HarmonyPrefix]
    private static void Prefix(
        PlayerManager __instance
    )
    {
        PlayerManagerCapacity.EnsureCapacity(
            __instance,
            "OnPlayerJoined"
        );
    }

    [HarmonyPostfix]
    private static void Postfix(
        PlayerManager __instance,
        PlayerRef player
    )
    {
        Plugin.Log.LogWarning(
            $"PLAYER STATE: " +
            $"joined={player}, " +
            $"MaxPlayerCount={__instance.MaxPlayerCount}, " +
            $"arrayLength={__instance._players?.Length ?? 0}"
        );

        if (__instance._players == null)
        {
            return;
        }

        for (
            int i = 0;
            i < __instance._players.Length;
            i++
        )
        {
            var entry = __instance._players[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.reference == PlayerRef.None)
            {
                continue;
            }

            Plugin.Log.LogWarning(
                $"PLAYER STATE [{i}]: " +
                $"ref={entry.reference}, " +
                $"playerObjectNull={entry.playerObject == null}, " +
                $"userNull={entry.user == null}, " +
                $"interactionNull=" +
                $"{entry.playerInteractionAgent == null}, " +
                $"partyNull={entry.playerParty == null}, " +
                $"colour=(" +
                $"{entry.color.r:F3}," +
                $"{entry.color.g:F3}," +
                $"{entry.color.b:F3}," +
                $"{entry.color.a:F3})"
            );
        }
    }
}


// Diagnostic: compare PlayerManager.Player.color with the
// colour ASKA calculates directly from PlayerCharacter.
[HarmonyPatch(
    typeof(PlayerManager),
    nameof(PlayerManager.SetPlayer),
    new[]
    {
        typeof(PlayerRef),
        typeof(NetworkObject)
    }
)]
internal static class PlayerSetDiagnosticPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        PlayerManager __instance,
        PlayerRef reference
    )
    {
        if (__instance._players == null)
        {
            return;
        }

        for (
            int i = 0;
            i < __instance._players.Length;
            i++
        )
        {
            var entry = __instance._players[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.reference != reference)
            {
                continue;
            }

            // SetPlayer is called multiple times while the
            // player record is still being initialized.
            if (entry.playerObject == null)
            {
                Plugin.Log.LogWarning(
                    $"PLAYER INIT PENDING: " +
                    $"slot={i}, " +
                    $"ref={reference}, " +
                    $"userNull={entry.user == null}, " +
                    $"colour=(" +
                    $"{entry.color.r:F3}," +
                    $"{entry.color.g:F3}," +
                    $"{entry.color.b:F3}," +
                    $"{entry.color.a:F3})"
                );

                return;
            }

            var character =
                entry.playerObject.GetComponent<PlayerCharacter>();

            if (character == null)
            {
                Plugin.Log.LogWarning(
                    $"PLAYER COLOUR DIAG: " +
                    $"slot={i}, " +
                    $"ref={reference}, " +
                    $"PlayerCharacter=NULL, " +
                    $"Player.color=(" +
                    $"{entry.color.r:F3}," +
                    $"{entry.color.g:F3}," +
                    $"{entry.color.b:F3}," +
                    $"{entry.color.a:F3})"
                );

                return;
            }

            try
            {
                int networkColourId =
                    character._GetNetworkColorID();

                var networkColour =
                    character.GetNetworkColor();

                Plugin.Log.LogWarning(
                    $"PLAYER COLOUR DIAG: " +
                    $"slot={i}, " +
                    $"ref={reference}, " +
                    $"helperNull=" +
                    $"{character.networkColorHelper == null}, " +
                    $"tableNull=" +
                    $"{character.networkColorTable == null}, " +
                    $"networkColourId={networkColourId}, " +
                    $"GetNetworkColor=(" +
                    $"{networkColour.r:F3}," +
                    $"{networkColour.g:F3}," +
                    $"{networkColour.b:F3}," +
                    $"{networkColour.a:F3}), " +
                    $"Player.color=(" +
                    $"{entry.color.r:F3}," +
                    $"{entry.color.g:F3}," +
                    $"{entry.color.b:F3}," +
                    $"{entry.color.a:F3})"
                );
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError(
                    $"PLAYER COLOUR DIAG FAILED: " +
                    $"slot={i}, " +
                    $"ref={reference}, " +
                    $"{ex}"
                );
            }

            return;
        }
    }
}