using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace RetakeExecutesPlugin.Memory;

/// <summary>
/// Copied directly from https://github.com/zwolof/cs2-executes/blob/master/Memory.cs
/// To say I understand exactly what it is is a lie. Its some kind of binary call to the underlying c++ layer
/// in order to get grenades spawn and bloom properly.
/// </summary>
public static class GrenadeHelper
{
    /// <summary>
    /// Throw smoke
    /// </summary>
    public static MemoryFunctionWithReturn<
        IntPtr,
        IntPtr,
        IntPtr,
        IntPtr,
        IntPtr,
        int,
        int,
        CSmokeGrenadeProjectile
    > CSmokeGrenadeProjectile_CreateFunc = new(
        Environment.OSVersion.Platform == PlatformID.Unix
            ? @"55 4C 89 C1 48 89 E5 41 57 45 89 CF 41 56 49 89 FE"
            : @"48 8B C4 48 89 58 ? 48 89 68 ? 48 89 70 ? 57 41 56 41 57 48 81 EC ? ? ? ? 48 8B B4 24 ? ? ? ? 4D 8B F8"
    );

    public static MemoryFunctionWithReturn<
        IntPtr,
        IntPtr,
        IntPtr,
        IntPtr,
        IntPtr,
        int,
        int,
        CFlashbangProjectile
    > CFlashbangProjectile_CreateFunc = new(
        Environment.OSVersion.Platform == PlatformID.Unix
            ? "55 4C 89 C1 48 89 E5 41 57 49 89 D7"
            : "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 48 83 EC 50 48 8B AC 24 80 00 00 00 49 8B F8"
    );
}
