namespace VNApproach;

/// <summary>Settings in config.json (created on first run).</summary>
public class ModConfig
{
    /// <summary>Master switch.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often (real seconds) rules are evaluated while nobody is approaching.</summary>
    public int CheckEverySeconds { get; set; } = 2;

    /// <summary>Walking speed while running to the player. Normal NPC speed is 2.</summary>
    public int RunSpeed { get; set; } = 4;

    /// <summary>Paths longer than this many tiles are ignored.</summary>
    public int MaxPathLength { get; set; } = 60;

    /// <summary>Minimum real seconds between two approaches, so NPCs don't pile on you.</summary>
    public int MinSecondsBetweenApproaches { get; set; } = 90;

    /// <summary>After the conversation (or a failed approach) the NPC walks back to the exact tile and facing it had before. Turn off only to debug.</summary>
    public bool ReturnToPlace { get; set; } = true;

    /// <summary>Walking speed on the way back.</summary>
    public int ReturnSpeed { get; set; } = 3;

    /// <summary>If the walk back takes longer than this many real seconds, the NPC is placed on its tile instead.</summary>
    public int ReturnTimeoutSeconds { get; set; } = 30;

    /// <summary>Never start an approach if the NPC's next schedule stop is within this many in-game minutes, so the schedule never fires while the NPC is away from its spot. 0 turns the guard off.</summary>
    public int ScheduleGuardMinutes { get; set; } = 40;

    /// <summary>Log every decision at Debug level.</summary>
    public bool Verbose { get; set; } = false;
}
