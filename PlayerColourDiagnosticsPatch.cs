using Fusion;
using HarmonyLib;
using SSSGame;
using UnityEngine;

namespace PlayerCapacityPatch;

[HarmonyPatch(
    typeof(PlayerManager),
    nameof(PlayerManager.SetPlayer),
    new[]
    {
        typeof(PlayerRef),
        typeof(NetworkObject)
    }
)]
internal static class PlayerColourDiagnosticsPatch
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

        for (int i = 0; i < __instance._players.Length; i++)
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

            // SetPlayer is called while the player is still
            // going through its initialization sequence.
            if (entry.playerObject == null)
            {
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
                    $"PlayerCharacter=NULL"
                );

                return;
            }

            var table =
                character.networkColorTable;

            if (table == null)
            {
                Plugin.Log.LogWarning(
                    $"PLAYER COLOUR DIAG: " +
                    $"slot={i}, " +
                    $"ref={reference}, " +
                    $"networkColorTable=NULL"
                );

                return;
            }

            try
            {
                int networkColourId =
                    character._GetNetworkColorID();

                int tableIndex =
                    networkColourId;

                Color tableColour =
                    table.GetColorAtIndex(
                        ref tableIndex
                    );

                int rawCount =
                    table.colors?.Count ?? 0;

                bool rawIndexValid =
                    networkColourId >= 0 &&
                    networkColourId < rawCount;

                Color rawColour =
                    rawIndexValid
                        ? table.colors[networkColourId]
                        : default;

                Color networkColour =
                    character.GetNetworkColor();

                Plugin.Log.LogWarning(
                    $"PLAYER COLOUR DIAG: " +
                    $"slot={i}, " +
                    $"ref={reference}, " +

                    $"tableName='{table.name}', " +

                    $"helperNull=" +
                    $"{character.networkColorHelper == null}, " +

                    $"networkColourId={networkColourId}, " +

                    $"ColorsCount={table.ColorsCount}, " +
                    $"rawCount={rawCount}, " +

                    $"rawColour=(" +
                    $"{rawColour.r:F3}," +
                    $"{rawColour.g:F3}," +
                    $"{rawColour.b:F3}," +
                    $"{rawColour.a:F3}), " +

                    $"GetColorAtIndex=(" +
                    $"{tableColour.r:F3}," +
                    $"{tableColour.g:F3}," +
                    $"{tableColour.b:F3}," +
                    $"{tableColour.a:F3}), " +

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