using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PKHeX.Core;

/// <summary>
/// Logic for checking shiny state.
/// </summary>
public static class ShinyUtil
{
    /// <summary>
    /// Computes a shiny PID from the provided values.
    /// </summary>
    /// <param name="tid">Trainer ID.</param>
    /// <param name="sid">Trainer Secret ID.</param>
    /// <param name="pid">Entity PID.</param>
    /// <param name="type">Shiny XOR type.</param>
    /// <returns>Shiny PID.</returns>
    public static uint GetShinyPID(in ushort tid, in ushort sid, in uint pid, in uint type)
    {
        var low = pid & 0xFFFF;
        return ((type ^ tid ^ sid ^ low) << 16) | low;
    }

    /// <summary>
    /// Checks if the PID is shiny.
    /// </summary>
    /// <param name="id32">Combined Trainer ID and Secret ID.</param>
    /// <param name="pid">Entity PID.</param>
    /// <param name="cmp">Comparison threshold.</param>
    /// <returns>True if shiny, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetIsShiny(in uint id32, in uint pid, [ConstantExpected(Max = 16, Min = 8)] uint cmp = 16) => GetShinyXor(id32, pid) < cmp;

    /// <summary>
    /// Computes the shiny XOR value.
    /// </summary>
    /// <param name="pid">Entity PID.</param>
    /// <param name="id32">Combined Trainer ID and Secret ID.</param>
    /// <returns>Shiny XOR value.</returns>
    public static uint GetShinyXor(in uint pid, in uint id32) => GetShinyXor(pid ^ id32);

    /// <summary>
    /// Computes the shiny XOR value.
    /// </summary>
    /// <param name="component">Combined/raw value to compute with.</param>
    /// <returns>Shiny XOR value.</returns>
    public static uint GetShinyXor(in uint component) => (component ^ (component >> 16)) & 0xFFFF;

    /// <summary>
    /// Forces the shiny state of the PID.
    /// </summary>
    /// <param name="isShiny">Indicates if the PID should be shiny.</param>
    /// <param name="pid">Entity PID.</param>
    /// <param name="id32">Combined Trainer ID and Secret ID.</param>
    /// <param name="xorType">Shiny XOR type.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ForceShinyState(bool isShiny, ref uint pid, uint id32, in uint xorType)
    {
        if (isShiny)
        {
            if (!GetIsShiny(id32, pid))
                pid = GetShinyPID((ushort)(id32 & 0xFFFFu), (ushort)(id32 >> 16), pid, xorType);
        }
        else
        {
            if (GetIsShiny(id32, pid))
                pid ^= 0x1000_0000;
        }
    }

    /// <summary>
    /// Checks if the PID is shiny.
    /// </summary>
    /// <remarks>Used for Gen 2.</remarks>
    /// <param name="dv16">16-bit DVs.</param>
    /// <returns>True if shiny, false otherwise.</returns>
    public static bool GetIsShinyGB(ushort dv16) => (dv16 & 0x2FFF) == 0x2AAA;
}
