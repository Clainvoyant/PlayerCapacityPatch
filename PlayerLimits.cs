using System;
using System.Diagnostics;

namespace PlayerCapacityPatch;

internal static class PlayerLimits
{
    internal const int DefaultMaxHumanPlayers = 16;
    internal const int MinimumMaxHumanPlayers = 4;
    internal const int MaximumMaxHumanPlayers = 32;

    internal static int MaxHumanPlayers { get; private set; } =
        DefaultMaxHumanPlayers;

    internal static void SetMaxHumanPlayers(int value)
    {
        MaxHumanPlayers = Math.Clamp(
            value,
            MinimumMaxHumanPlayers,
            MaximumMaxHumanPlayers
        );
    }

    // ASKA internally needs one extra Player record.
    internal static int PlayerManagerCapacity =>
        IsDedicatedServer
            ? MaxHumanPlayers + 1
            : MaxHumanPlayers;

    internal static bool IsDedicatedServer
    {
        get
        {
            string processName =
                Process.GetCurrentProcess().ProcessName;

            return processName.Equals(
                "AskaServer",
                StringComparison.OrdinalIgnoreCase
            );
        }
    }

    // Dedicated servers consume one of the network/lobby slots.
    internal static int NetworkCapacity =>
        IsDedicatedServer
            ? MaxHumanPlayers + 1
            : MaxHumanPlayers;
}