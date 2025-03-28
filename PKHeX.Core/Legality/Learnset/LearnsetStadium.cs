using System;
using System.Runtime.InteropServices;
using static PKHeX.Core.LearnSourceStadium;

namespace PKHeX.Core;

/// <summary>
/// Stadium 2's Move Reminder
/// </summary>
/// <remarks>
/// Entries are sorted by ascending level; so the first entry for a move is the minimum level to learn it.
/// Moves may appear multiple times in the learnset, but only the first needs to be satisfied to be "valid" for Stadium 2's checks.
/// https://bluemoonfalls.com/pages/general/move-reminder
/// </remarks>
public sealed class LearnsetStadium
{
    private readonly StadiumTuple[] Learn;
    public LearnsetStadium(ReadOnlySpan<byte> input)
        => Learn = MemoryMarshal.Cast<byte, StadiumTuple>(input).ToArray();

    /// <summary> Gets all entries. </summary>
    public ReadOnlySpan<StadiumTuple> GetMoves() => Learn;

    /// <summary>
    /// Gets the move info for a specific move ID.
    /// </summary>
    /// <param name="move">Move ID</param>
    /// <param name="result">Move info for the specified move ID.</param>
    /// <returns>True if the move exists in the learnset. Will return the minimum level move entry if multiple entries exist.</returns>
    public bool TryGetMove(ushort move, out StadiumTuple result)
    {
        foreach (var learn in Learn)
        {
            if (move != learn.Move)
                continue;
            result = learn;
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>
    /// Checks if the move can be learned at the specified level.
    /// </summary>
    /// <param name="move">Move ID</param>
    /// <param name="level">Current level of the Pokémon.</param>
    /// <returns></returns>
    public bool CanKnow(ushort move, byte level) => TryGetMove(move, out var result) && result.Level <= level;

    /// <summary>
    /// Checks if the move can be relearned by Stadium 2 to the current moves.
    /// </summary>
    /// <param name="move">Move ID to try and remember.</param>
    /// <param name="level">Current level of the Pokémon.</param>
    /// <returns>True if the move can be relearned.</returns>
    public bool CanRelearn(ushort move, byte level)
    {
        foreach (var learn in Learn)
        {
            if (level < learn.Level)
                return false; // moves are sorted by level; eager return.
            if (move != learn.Move)
                continue;
            if (!learn.Source.IsAbleToBeRelearned())
                continue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks the moveset.
    /// </summary>
    /// <remarks>Smeargle should be checked separately.</remarks>
    /// <param name="moves">Currently known moves.</param>
    /// <param name="level">Current level of the Pokémon.</param>
    /// <param name="flag">Invalid moves will be marked as true in this span.</param>
    /// <returns>True if all moves are valid.</returns>
    public bool Validate(ReadOnlySpan<ushort> moves, byte level, Span<bool> flag)
    {
        bool anyInvalid = false;

        // todo: is stadium smart to disallow egg moves+event, or multiple event moves (pikachu)?
        // Naive checker only checking individual moves in isolation.
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            if (move == 0)
                continue; // skip empty moves
            if (CanKnow(move, level))
                continue;
            if (i >= flag.Length)
                break; // avoid out of bounds, shouldn't happen but just in case
            anyInvalid = flag[i] = true;
        }
        return !anyInvalid;
    }

    /// <summary>
    /// Converts a <see cref="BinLinkerAccessor"/> into an array of <see cref="LearnsetStadium"/>.
    /// </summary>
    public static LearnsetStadium[] GetArray(BinLinkerAccessor entries)
    {
        var result = new LearnsetStadium[entries.Length];
        for (int i = 0; i < entries.Length; i++)
            result[i] = new LearnsetStadium(entries[i]);
        return result;
    }
}

/// <summary>
/// Value tuple for Stadium 2's move reminder.
/// </summary>
/// <param name="Level">Minimum level to learn the move.</param>
/// <param name="Move">>Move ID.</param>
/// <param name="Source">>Source of the move (e.g., level up, tutor, egg).</param>
public readonly record struct StadiumTuple(byte Level, byte Move, LearnSourceStadium Source)
{
    public override string ToString() => $"Lv{Level} {(Move)Move} // {Source}";
}

/// <summary>
/// Flags for the source of moves learned in Stadium 2.
/// </summary>
[Flags]
public enum LearnSourceStadium : byte
{
    None,
    LevelUpRB = 1 << 0,
    LevelUpYW = 1 << 1,
    LevelUpGS = 1 << 2,
    LevelUpC  = 1 << 3,
    TutorC    = 1 << 4,
    EggC      = 1 << 5,
    EggGS     = 1 << 6,
    Event     = 1 << 7,
}

public static class LearnSourceStadiumExtensions
{
    /// <summary>
    /// Checks if the source can be relearned.
    /// </summary>
    public static bool IsAbleToBeRelearned(this LearnSourceStadium source) => (source & (LevelUpRB | LevelUpYW | LevelUpGS | LevelUpC)) != 0;
}
