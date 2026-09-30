using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PlayerCapacityPatch;

[BepInPlugin(
    "clainvoyant.playercapacitypatch",
    "Player Capacity Patch",
    "1.0.0"
)]
public class Plugin : BasePlugin
{
    internal static new BepInEx.Logging.ManualLogSource Log = null!;

    public override void Load()
    {
        Log = base.Log;

        var maxPlayers = Config.Bind(
            "Player Capacity",
            "MaxPlayers",
            16,
            new ConfigDescription(
                "Maximum human players allowed in multiplayer sessions (4-32).",
                new AcceptableValueRange<int>(4, 32)
            )
        );
        PlayerLimits.SetMaxHumanPlayers(maxPlayers.Value);

        PlayerColours.BindExtraColourConfig(Config);

        Log.LogInfo("Player Capacity Patch loading...");

        var harmony = new Harmony("clainvoyant.playercapacitypatch");
        harmony.PatchAll();

        Log.LogInfo("Harmony patches installed.");
    }
}