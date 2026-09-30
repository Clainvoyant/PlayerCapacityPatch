//using Fusion;
//using HarmonyLib;
//using SSSGame;
//using UnityEngine;
//
//namespace PlayerCapacityPatch;
//
//
//// If ASKA returns transparent black for a network colour,
//// fall back to its own colour ID and colour table.
//[HarmonyPatch(
//    typeof(PlayerCharacter),
//    nameof(PlayerCharacter.GetNetworkColor)
//)]
//internal static class PlayerNetworkColourFixPatch
//{
//    [HarmonyPostfix]
//    private static void Postfix(
//        PlayerCharacter __instance,
//        ref Color __result
//    )
//    {
//        // ASKA returned a valid colour.
//        if (__result.a > 0.001f)
//        {
//            return;
//        }
//
//        var table =
//            __instance.networkColorTable;
//
//        if (table == null)
//        {
//            return;
//        }
//
//        int colourId =
//            __instance._GetNetworkColorID();
//
//        if (
//            colourId < 0 ||
//            colourId >= table.ColorsCount
//        )
//        {
//            return;
//        }
//
//        int tableIndex =
//            colourId;
//
//        __result =
//            table.GetColorAtIndex(
//                ref tableIndex
//            );
//    }
//}
//
//
//// On affected clients GetNetworkColor() can now return
//// correctly, but PlayerManager.Player.color remains clear.
//// Copy the repaired colour into the PlayerManager record.
//[HarmonyPatch(
//    typeof(PlayerManager),
//    nameof(PlayerManager.SetPlayer),
//    new[]
//    {
//        typeof(PlayerRef),
//        typeof(NetworkObject)
//    }
//)]
//internal static class PlayerManagerColourFixPatch
//{
//    [HarmonyPostfix]
//    private static void Postfix(
//        PlayerManager __instance,
//        PlayerRef reference
//    )
//    {
//        if (__instance._players == null)
//        {
//            return;
//        }
//
//        for (
//            int i = 0;
//            i < __instance._players.Length;
//            i++
//        )
//        {
//            var entry =
//                __instance._players[i];
//
//            if (entry == null)
//            {
//                continue;
//            }
//
//            if (entry.reference != reference)
//            {
//                continue;
//            }
//
//            if (entry.playerObject == null)
//            {
//                return;
//            }
//
//            // Already initialized correctly.
//            if (entry.color.a > 0.001f)
//            {
//                return;
//            }
//
//            var character =
//                entry.playerObject
//                    .GetComponent<PlayerCharacter>();
//
//            if (character == null)
//            {
//                return;
//            }
//
//            Color networkColour =
//                character.GetNetworkColor();
//
//            if (networkColour.a <= 0.001f)
//            {
//                return;
//            }
//
//entry.color =
//    networkColour;
//
//var helper =
//    character.networkColorHelper;
//
//if (helper != null)
//{
//    var materialColour =
//        networkColour;
//
//    helper.SetColor(
//        ref materialColour
//    );
//
//    Plugin.Log.LogInfo(
//        $"Player visual colour reapplied: " +
//        $"slot={i}, " +
//        $"ref={reference}, " +
//        $"colour=(" +
//        $"{materialColour.r:F3}," +
//        $"{materialColour.g:F3}," +
//        $"{materialColour.b:F3}," +
//        $"{materialColour.a:F3})"
//    );
//}
//
//            Plugin.Log.LogInfo(
//                $"Player colour repaired: " +
//                $"slot={i}, " +
//                $"ref={reference}, " +
//                $"colour=(" +
//                $"{networkColour.r:F3}," +
//                $"{networkColour.g:F3}," +
//                $"{networkColour.b:F3}," +
//                $"{networkColour.a:F3})"
//            );
//
//            return;
//        }
//    }
//}

using HarmonyLib;
using SSSGame;
using UnityEngine;
using Fusion;
using SandSailorStudio.Utils;
using BepInEx.Configuration;
using System.Globalization;

namespace PlayerCapacityPatch;

internal static class PlayerColours
{
    internal static readonly Color[] ExtraColours =
    {
        new(1.00f, 0.20f, 0.65f, 1.00f), // 4  Magenta
        new(0.00f, 0.90f, 1.00f, 1.00f), // 5  Cyan
        new(1.00f, 0.45f, 0.05f, 1.00f), // 6  Orange
        new(0.65f, 0.25f, 1.00f, 1.00f), // 7  Purple
        new(0.65f, 1.00f, 0.10f, 1.00f), // 8  Lime
        new(0.00f, 0.65f, 0.55f, 1.00f), // 9  Teal
        new(1.00f, 0.35f, 0.35f, 1.00f), // 10 Coral
        new(0.85f, 0.35f, 1.00f, 1.00f), // 11 Violet
        new(0.30f, 0.75f, 1.00f, 1.00f), // 12 Sky
        new(1.00f, 0.65f, 0.00f, 1.00f), // 13 Gold
        new(0.35f, 1.00f, 0.70f, 1.00f), // 14 Mint
        new(0.75f, 0.50f, 0.25f, 1.00f), // 15 Brown
    };

    internal static void BindExtraColourConfig(
        ConfigFile config
    )
    {
        for (int i = 0; i < ExtraColours.Length; i++)
        {
            var defaultColour = ExtraColours[i];
            string defaultValue = FormatColour(defaultColour);
            string description = i == 0
                ? "Color for network color ID 4 (Magenta). Use #RRGGBB or #RRGGBBAA, or normalized R,G,B[,A] values from 0 to 1."
                : string.Empty;

            var entry = config.Bind(
                "ExtraPlayerColors",
                $"Player{i + 5}",
                defaultValue,
                description
            );

            if (TryParseColour(entry.Value, out Color configuredColour))
            {
                ExtraColours[i] = configuredColour;
            }
            else
            {
                Plugin.Log.LogWarning(
                    $"Invalid ExtraPlayerColors.Player{i + 5} value " +
                    $"'{entry.Value}', keeping default {defaultValue}."
                );
            }
        }
    }

    internal static Color GetColour(
        PlayerCharacter character
    )
    {
        int id =
            character._GetNetworkColorID();

        var table =
            character.networkColorTable;

        // Preserve the original ASKA colours exactly.
        if (
            table != null &&
            id >= 0 &&
            id < table.colors.Count
        )
        {
            return table.colors[id];
        }

        // Our additional player colours.
        if (TryGetExtraColour(id, out Color extraColour))
        {
            return extraColour;
        }

        return Color.white;
    }

    internal static bool TryGetExtraColour(
        int id,
        out Color colour
    )
    {
        int extraIndex = id - 4;
        if (extraIndex >= 0 && extraIndex < ExtraColours.Length)
        {
            colour = ExtraColours[extraIndex];
            return true;
        }

        colour = default;
        return false;
    }

    private static string FormatColour(Color colour)
    {
        return string.Join(
            ",",
            colour.r.ToString("F2", CultureInfo.InvariantCulture),
            colour.g.ToString("F2", CultureInfo.InvariantCulture),
            colour.b.ToString("F2", CultureInfo.InvariantCulture),
            colour.a.ToString("F2", CultureInfo.InvariantCulture)
        );
    }

    private static bool TryParseColour(
        string value,
        out Color colour
    )
    {
        colour = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();
        if (ColorUtility.TryParseHtmlString(value, out colour))
        {
            return true;
        }

        string[] components = value.Split(',');
        if (components.Length < 3 || components.Length > 4)
        {
            return false;
        }

        var channels = new float[components.Length];
        for (int i = 0; i < components.Length; i++)
        {
            if (
                !float.TryParse(
                    components[i].Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out channels[i]
                ) ||
                float.IsNaN(channels[i]) ||
                float.IsInfinity(channels[i]) ||
                channels[i] < 0f ||
                channels[i] > 1f
            )
            {
                return false;
            }
        }

        colour = new Color(
            channels[0],
            channels[1],
            channels[2],
            components.Length == 4 ? channels[3] : 1f
        );

        return true;
    }
}

[HarmonyPatch(
    typeof(ColorTableConfig),
    nameof(ColorTableConfig.TryGetColor)
)]
internal static class PlayerNetworkColorTablePatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        ColorTableConfig __instance,
        ref int index,
        ref Color color,
        ref bool __result
    )
    {
        if (__instance == null || __instance.name != "PlayerNetworkColors")
        {
            return true;
        }

        if (
            index < __instance.ColorsCount ||
            !PlayerColours.TryGetExtraColour(index, out Color extraColour)
        )
        {
            return true;
        }

        color = extraColour;
        __result = true;

        Plugin.Log.LogInfo(
            $"Extra network color lookup: " +
            $"table={__instance.name}, id={index}, " +
            $"color=({color.r:F3},{color.g:F3},{color.b:F3},{color.a:F3})"
        );

        return false;
    }
}


[HarmonyPatch(
    typeof(PlayerCharacter),
    nameof(PlayerCharacter.GetNetworkColor)
)]
internal static class PlayerCharacterNetworkColourPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        PlayerCharacter __instance,
        ref Color __result
    )
    {
        __result =
            PlayerColours.GetColour(
                __instance
            );

        // Skip ASKA's native GetNetworkColor().
        return false;
    }
}

[HarmonyPatch(
    typeof(PlayerManager),
    nameof(PlayerManager.SetPlayer),
    new[]
    {
        typeof(PlayerRef),
        typeof(NetworkObject)
    }
)]
internal static class PlayerCharacterVisualColourPatch
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
            var player =
                __instance._players[i];

            if (
                player == null ||
                player.reference != reference ||
                player.playerObject == null
            )
            {
                continue;
            }

            var character =
                player.playerObject
                    .GetComponent<PlayerCharacter>();

            if (character == null)
            {
                return;
            }

            Color colour =
                PlayerColours.GetColour(
                    character
                );

            player.color =
                colour;

            if (character.networkColorHelper != null)
            {
                character.networkColorHelper.SetColor(
                    ref colour
                );
            }

            Plugin.Log.LogInfo(
                $"Player colour applied: " +
                $"slot={i}, " +
                $"ref={reference}, " +
                $"id={character._GetNetworkColorID()}, " +
                $"colour=(" +
                $"{colour.r:F3}," +
                $"{colour.g:F3}," +
                $"{colour.b:F3}," +
                $"{colour.a:F3})"
            );

            return;
        }
    }
}