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
        new(0.95f, 0.10f, 0.10f, 1.00f), // 16 Crimson
        new(0.10f, 0.35f, 0.95f, 1.00f), // 17 Royal Blue
        new(0.95f, 0.85f, 0.10f, 1.00f), // 18 Lemon
        new(0.10f, 0.75f, 0.20f, 1.00f), // 19 Emerald
        new(0.95f, 0.10f, 0.85f, 1.00f), // 20 Fuchsia
        new(0.10f, 0.80f, 0.85f, 1.00f), // 21 Turquoise
        new(0.95f, 0.55f, 0.35f, 1.00f), // 22 Salmon
        new(0.45f, 0.20f, 0.10f, 1.00f), // 23 Umber
        new(0.45f, 0.95f, 0.95f, 1.00f), // 24 Ice
        new(0.55f, 0.10f, 0.25f, 1.00f), // 25 Wine
        new(0.35f, 0.45f, 0.10f, 1.00f), // 26 Olive
        new(0.40f, 0.25f, 0.85f, 1.00f), // 27 Indigo
        new(0.90f, 0.40f, 0.65f, 1.00f), // 28 Rose
        new(0.55f, 0.55f, 0.60f, 1.00f), // 29 Silver
        new(0.15f, 0.15f, 0.20f, 1.00f), // 30 Charcoal
        new(0.85f, 0.85f, 0.80f, 1.00f), // 31 Ivory
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
                ? "Color for network color ID 4 (Magenta). Player5 through Player32 map to IDs 4 through 31. Use #RRGGBB or #RRGGBBAA, or normalized R,G,B[,A] values from 0 to 1."
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