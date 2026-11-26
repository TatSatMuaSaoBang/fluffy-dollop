using Microsoft.Xna.Framework;

namespace VNApproach;

/// <summary>One entry of the Mods/Fouky.VNApproach/Rules asset. Content Patcher packs edit that asset.</summary>
public class ApproachRule
{
    /// <summary>Internal NPC names this rule covers. "*" or an empty list means every NPC.</summary>
    public List<string> Npcs { get; set; } = new();

    /// <summary>NPCs to skip when Npcs is "*".</summary>
    public List<string>? ExcludeNpcs { get; set; }

    /// <summary>Game state query. {NpcName} (or {{NpcName}}) becomes the NPC's internal name. Empty means always true.</summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// true: a conversation is waiting, so the NPC comes as soon as Conditions are true (Chance is ignored).
    /// false: optional, rolled with Chance.
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>For optional rules: chance per real-time MINUTE that the NPC comes while Conditions hold.</summary>
    public double Chance { get; set; } = 0.1;

    /// <summary>Day (once per day per NPC, default), Ever (once per save per NPC), or None.</summary>
    public string Cooldown { get; set; } = "Day";

    /// <summary>Higher goes first when several rules are true. Required rules always beat optional ones.</summary>
    public int Priority { get; set; } = 0;

    /// <summary>Only in these locations (internal names, or "Outdoors" / "Indoors"). Empty means anywhere.</summary>
    public List<string>? Locations { get; set; }

    /// <summary>Never in these locations.</summary>
    public List<string>? ExcludeLocations { get; set; }

    /// <summary>Give up after this many real seconds without reaching the player.</summary>
    public int TimeoutSeconds { get; set; } = 25;

    /// <summary>Speech bubble when the NPC starts running. A random line is used. "@" is the player's name.</summary>
    public List<string>? StartLines { get; set; }

    /// <summary>Emote when starting: Question, Angry, Exclamation, Heart, Sleep, Sad, Happy, Music, Blush or a number.</summary>
    public string? StartEmote { get; set; } = "Exclamation";

    /// <summary>Emote on arrival.</summary>
    public string? ArriveEmote { get; set; }

    /// <summary>Speech bubble on arrival.</summary>
    public List<string>? ArriveLines { get; set; }

    /// <summary>On arrival, act like the player pressed the action button on the NPC. With Character Interact installed that opens its menu.</summary>
    public bool OpenMenu { get; set; } = true;

    public bool AppliesTo(string npcName)
    {
        if (this.ExcludeNpcs?.Any(n => n.Equals(npcName, StringComparison.OrdinalIgnoreCase)) == true)
            return false;
        if (this.Npcs == null || this.Npcs.Count == 0)
            return true;
        return this.Npcs.Any(n => n == "*" || n.Equals(npcName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Where the running approach is: heading to the player, talking, or walking back to its spot.</summary>
internal enum ApproachPhase
{
    Running,
    Talking,
    Returning
}

/// <summary>The approach that is currently running (only one at a time).</summary>
internal class ActiveApproach
{
    public string RuleId { get; }
    public ApproachRule Rule { get; }
    public StardewValley.NPC Npc { get; }
    public int OriginalSpeed { get; }
    public DateTime StartedUtc { get; } = DateTime.UtcNow;
    public Point Target { get; set; }

    /// <summary>Where the NPC stood before it left, so it can go back and its schedule stays intact.</summary>
    public StardewValley.GameLocation OriginLocation { get; }
    public Point OriginTile { get; }
    public int OriginFacing { get; }

    public ApproachPhase Phase { get; set; } = ApproachPhase.Running;
    public DateTime PhaseStartedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>True once the NPC reached the player (the conversation was delivered).</summary>
    public bool Delivered { get; set; }

    /// <summary>Talking phase: a menu or dialogue has been seen open.</summary>
    public bool MenuSeen { get; set; }

    /// <summary>Talking phase: consecutive seconds with nothing open.</summary>
    public int QuietSeconds { get; set; }

    public int ReturnAttempts { get; set; }

    public ActiveApproach(string ruleId, ApproachRule rule, StardewValley.NPC npc)
    {
        this.RuleId = ruleId;
        this.Rule = rule;
        this.Npc = npc;
        this.OriginalSpeed = npc.speed;
        this.OriginLocation = npc.currentLocation;
        this.OriginTile = npc.TilePoint;
        this.OriginFacing = npc.FacingDirection;
    }
}
