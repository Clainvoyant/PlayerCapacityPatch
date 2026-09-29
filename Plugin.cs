using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PlayerCapacityPatch;

[BepInPlugin(
    "com.abaddon.aska.testmod",
    "ASKA Test Mod",
    "0.2.0"
)]
public class Plugin : BasePlugin
{
    internal static new BepInEx.Logging.ManualLogSource Log = null!;

    public override void Load()
    {
        Log = base.Log;

        Log.LogInfo("ASKA Test Mod loading...");

        var harmony = new Harmony("com.abaddon.aska.testmod");
        harmony.PatchAll();

        Log.LogInfo("Harmony patches installed.");
    }
}