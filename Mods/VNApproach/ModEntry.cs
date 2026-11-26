using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Pathfinding;

namespace VNApproach;

public class ModEntry : Mod
{
    public const string RulesAsset = "Mods/Fouky.VNApproach/Rules";
    private const string ModDataPrefix = "Fouky.VNApproach/";

    private ModConfig Config = null!;
    private ActiveApproach? Active;
    private int SecondsSinceCheck;
    private DateTime NextAllowedUtc = DateTime.MinValue;
    private readonly Dictionary<string, DateTime> NpcRetryUtc = new();
    private readonly Random Rng = new();

    /// <summary>Rules and letters shipped with this mod (assets/rules.json, assets/mail.json).</summary>
    private Dictionary<string, ApproachRule> BuiltInRules = new();
    private Dictionary<string, string> BuiltInMail = new();

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();

        this.BuiltInRules = this.ReadAsset<Dictionary<string, ApproachRule>>("assets/rules.json") ?? new();
        this.BuiltInMail = this.ReadAsset<Dictionary<string, string>>("assets/mail.json") ?? new();

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecond;

        // New day: the game resets every NPC to its day start, so only drop our state.
        helper.Events.GameLoop.DayStarted += (_, _) => this.CancelSilently();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => this.Active = null;

        // Player left the room: nobody can see the NPC, so put it back on its own tile.
        helper.Events.Player.Warped += (_, e) =>
        {
            if (e.IsLocalPlayer)
                this.CancelPlayerLeft();
        };

        helper.ConsoleCommands.Add("vn_approach", "Make an NPC in your location run to you, talk, then walk back.\nUsage: vn_approach <npc> [openMenu true|false] [force]", this.CmdApproach);
        helper.ConsoleCommands.Add("vn_approach_rules", "Show how every rule evaluates for the NPCs in your location.", this.CmdRules);
    }

    /*********
    ** Data
    *********/
    private T? ReadAsset<T>(string path) where T : class
    {
        try
        {
            return this.Helper.Data.ReadJsonFile<T>(path);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Could not read {path}: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // The rules asset starts with this mod's own rules. Other Content Patcher packs can still edit it.
        if (e.NameWithoutLocale.IsEquivalentTo(RulesAsset))
            e.LoadFrom(() => new Dictionary<string, ApproachRule>(this.BuiltInRules), AssetLoadPriority.Exclusive);

        // The story letters.
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail") && this.BuiltInMail.Count > 0)
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                foreach (var pair in this.BuiltInMail)
                    data[pair.Key] = pair.Value;
            });
        }
    }

    private Dictionary<string, ApproachRule> LoadRules()
    {
        return this.Helper.GameContent.Load<Dictionary<string, ApproachRule>>(RulesAsset);
    }

    /*********
    ** Main loop (once per real second)
    *********/
    private void OnOneSecond(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        if (!this.Config.Enabled || !Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (this.Active != null)
        {
            this.TickActive(this.Active);
            return;
        }

        this.SecondsSinceCheck++;
        if (this.SecondsSinceCheck < Math.Max(1, this.Config.CheckEverySeconds))
            return;
        this.SecondsSinceCheck = 0;

        this.TryStart();
    }

    private void TryStart()
    {
        if (DateTime.UtcNow < this.NextAllowedUtc || !Context.IsPlayerFree || Game1.isFestival())
            return;

        GameLocation? location = Game1.currentLocation;
        if (location == null)
            return;

        Dictionary<string, ApproachRule> rules = this.LoadRules();
        if (rules.Count == 0)
            return;

        List<NPC> npcs = location.characters.ToList();
        foreach (var pair in rules.OrderByDescending(r => r.Value.Required).ThenByDescending(r => r.Value.Priority))
        {
            ApproachRule rule = pair.Value;
            if (!LocationAllowed(rule, location))
                continue;

            double perCheck = 1 - Math.Pow(1 - Math.Clamp(rule.Chance, 0, 1), Math.Max(1, this.Config.CheckEverySeconds) / 60.0);

            foreach (NPC npc in npcs)
            {
                if (!rule.AppliesTo(npc.Name) || !this.CanApproach(npc) || this.OnCooldown(pair.Key, rule, npc))
                    continue;
                if (!rule.Required && this.Rng.NextDouble() > perCheck)
                    continue;
                if (!ConditionsMet(rule, npc))
                    continue;

                if (this.BeginApproach(pair.Key, rule, npc))
                    return;

                // no route right now: don't ask this NPC again for a while
                this.NpcRetryUtc[npc.Name] = DateTime.UtcNow.AddSeconds(30);
            }
        }
    }

    private bool CanApproach(NPC npc)
    {
        if (npc.currentLocation != Game1.currentLocation || npc.IsInvisible || !npc.IsVillager || npc.isSleeping.Value)
            return false;
        // already walking a schedule or another scripted path
        if (npc.controller != null || npc.temporaryController != null)
            return false;
        if (this.NpcRetryUtc.TryGetValue(npc.Name, out DateTime retry) && DateTime.UtcNow < retry)
            return false;
        // a schedule stop is about to start: leave the NPC alone so the schedule fires from its own spot
        if (this.ScheduleTooClose(npc))
            return false;
        return true;
    }

    /*********
    ** Schedule safety
    *********/
    /// <summary>True when the NPC's next schedule stop is within ScheduleGuardMinutes in-game minutes.</summary>
    private bool ScheduleTooClose(NPC npc)
    {
        int guard = this.Config.ScheduleGuardMinutes;
        if (guard <= 0)
            return false;

        int now = ToMinutes(Game1.timeOfDay);
        foreach (int time in GetScheduleTimes(npc))
        {
            int diff = ToMinutes(time) - now;
            if (diff > 0 && diff <= guard)
                return true;
        }
        return false;
    }

    private static int ToMinutes(int timeOfDay) => timeOfDay / 100 * 60 + timeOfDay % 100;

    /// <summary>The times (e.g. 900, 1330) of today's schedule stops. Read by reflection so it works whether the game exposes a property or a field.</summary>
    private static IEnumerable<int> GetScheduleTimes(NPC npc)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = npc.GetType();
        object? raw = type.GetProperty("Schedule", flags)?.GetValue(npc)
            ?? type.GetField("schedule", flags)?.GetValue(npc);

        if (raw is IDictionary dict)
        {
            foreach (object key in dict.Keys)
            {
                if (key is int time)
                    yield return time;
            }
        }
    }

    /*********
    ** Starting and running
    *********/
    private bool BeginApproach(string ruleId, ApproachRule rule, NPC npc)
    {
        var approach = new ActiveApproach(ruleId, rule, npc);

        // already standing next to the player: skip the run
        if (Vector2.Distance(npc.Tile, Game1.player.Tile) <= 1.5f)
        {
            this.Active = approach;
            this.Arrive(approach);
            return true;
        }

        if (!this.TryPath(approach))
        {
            this.Log($"{npc.Name}: no path to the player for rule '{ruleId}'.");
            return false;
        }

        npc.speed = Math.Max(npc.speed, this.Config.RunSpeed);
        this.Active = approach;
        this.Log($"{npc.Name} starts running to the player (rule '{ruleId}'), from tile {approach.OriginTile.X},{approach.OriginTile.Y}.");

        ShowEmote(npc, rule.StartEmote);
        if (rule.StartLines is { Count: > 0 })
            npc.showTextAboveHead(this.Pick(rule.StartLines));
        return true;
    }

    /// <summary>Try the four tiles around the player; use the first one with a valid, short enough route.</summary>
    private bool TryPath(ActiveApproach approach)
    {
        NPC npc = approach.Npc;
        Point p = Game1.player.TilePoint;

        // (tile, direction the NPC should face when it gets there)
        var options = new List<(Point Tile, int Face)>
        {
            (new Point(p.X, p.Y + 1), 0),
            (new Point(p.X - 1, p.Y), 1),
            (new Point(p.X + 1, p.Y), 3),
            (new Point(p.X, p.Y - 1), 2)
        };

        foreach (var option in options.OrderBy(o => Vector2.DistanceSquared(npc.Tile, o.Tile.ToVector2())))
        {
            try
            {
                var controller = new PathFindController(npc, npc.currentLocation, option.Tile, option.Face);
                var path = controller.pathToEndPoint;
                if (path != null && path.Count > 0 && path.Count <= this.Config.MaxPathLength)
                {
                    npc.temporaryController = controller;
                    approach.Target = option.Tile;
                    return true;
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Pathfinding threw for {npc.Name}: {ex.Message}", LogLevel.Trace);
            }
        }

        return false;
    }

    private void TickActive(ActiveApproach a)
    {
        NPC npc = a.Npc;

        // The game moved the NPC somewhere else (event, festival, warp): it owns the NPC now, leave it alone.
        if (npc.currentLocation != a.OriginLocation || npc.IsInvisible || Game1.CurrentEvent != null || Game1.isFestival())
        {
            this.Log($"{npc.Name}: the game took over (event, festival or warp). Leaving the NPC alone.");
            this.NpcRetryUtc[npc.Name] = DateTime.UtcNow.AddSeconds(30);
            this.Finish(a, halt: false);
            return;
        }

        // The player is no longer in the room.
        if (npc.currentLocation != Game1.currentLocation)
        {
            this.CancelPlayerLeft();
            return;
        }

        switch (a.Phase)
        {
            case ApproachPhase.Running:
                this.TickRunning(a);
                break;
            case ApproachPhase.Talking:
                this.TickTalking(a);
                break;
            case ApproachPhase.Returning:
                this.TickReturning(a);
                break;
        }
    }

    private void TickRunning(ActiveApproach a)
    {
        NPC npc = a.Npc;

        // A schedule stop started while we were running: the schedule owns the NPC from here.
        if (npc.controller != null)
        {
            this.Log($"{npc.Name}: a schedule stop started during the run. Handing the NPC back.");
            this.NpcRetryUtc[npc.Name] = DateTime.UtcNow.AddSeconds(30);
            this.Finish(a, halt: false);
            return;
        }

        if ((DateTime.UtcNow - a.StartedUtc).TotalSeconds > a.Rule.TimeoutSeconds)
        {
            this.Log($"{npc.Name} gave up (timeout).");
            this.Abort(a, 60);
            return;
        }

        float distance = Vector2.Distance(npc.Tile, Game1.player.Tile);
        if (distance <= 1.5f)
        {
            // wait until the player isn't in a menu or cutscene; the timeout covers a long wait
            if (Context.IsPlayerFree)
                this.Arrive(a);
            return;
        }

        bool pathEnded = npc.temporaryController == null;
        bool playerMoved = Vector2.Distance(a.Target.ToVector2(), Game1.player.Tile) > 1.5f;
        if ((pathEnded || playerMoved) && !this.TryPath(a))
            this.Abort(a, 30);
    }

    /*********
    ** Arriving and talking
    *********/
    private void Arrive(ActiveApproach a)
    {
        NPC npc = a.Npc;

        npc.temporaryController = null;
        npc.Halt();
        npc.speed = a.OriginalSpeed;

        a.Phase = ApproachPhase.Talking;
        a.PhaseStartedUtc = DateTime.UtcNow;
        a.Delivered = true;
        a.MenuSeen = false;
        a.QuietSeconds = 0;

        npc.faceGeneralDirection(Game1.player.getStandingPosition());
        Game1.player.faceGeneralDirection(npc.getStandingPosition());
        ShowEmote(npc, a.Rule.ArriveEmote);
        if (a.Rule.ArriveLines is { Count: > 0 })
            npc.showTextAboveHead(this.Pick(a.Rule.ArriveLines));

        this.MarkDone(a.RuleId, a.Rule, npc);
        this.Log($"{npc.Name} reached the player.");

        if (a.Rule.OpenMenu)
        {
            DelayedAction.functionAfterDelay(() =>
            {
                if (this.Active == a && Context.IsPlayerFree && npc.currentLocation == Game1.currentLocation)
                    npc.checkAction(Game1.player, npc.currentLocation);
            }, 600);
        }
    }

    /// <summary>Wait until the conversation is over, then send the NPC back.</summary>
    private void TickTalking(ActiveApproach a)
    {
        double seconds = (DateTime.UtcNow - a.PhaseStartedUtc).TotalSeconds;
        if (seconds < 1.5)
            return; // the menu opens after a short delay

        bool busy = !Context.IsPlayerFree;
        if (busy)
        {
            a.MenuSeen = true;
            a.QuietSeconds = 0;
            return;
        }

        a.QuietSeconds++;

        // Menu seen and now closed for 2 seconds in a row (a scene can briefly have nothing open between two boxes).
        // Menu never seen: give it a few seconds, then carry on (no menu rule, or the menu failed to open).
        double giveUp = a.Rule.OpenMenu ? 6 : 3;
        bool over = a.MenuSeen ? a.QuietSeconds >= 2 : seconds >= giveUp;
        if (over)
            this.BeginReturn(a);
    }

    /*********
    ** Going back to the schedule spot
    *********/
    /// <summary>Stop the run (or the talk) and put the NPC back where it was, so its schedule continues untouched.</summary>
    private void Abort(ActiveApproach a, int retrySeconds)
    {
        this.NpcRetryUtc[a.Npc.Name] = DateTime.UtcNow.AddSeconds(Math.Max(1, retrySeconds));
        this.NextAllowedUtc = DateTime.UtcNow.AddSeconds(Math.Max(10, retrySeconds / 2));
        this.BeginReturn(a);
    }

    private void BeginReturn(ActiveApproach a)
    {
        NPC npc = a.Npc;
        npc.temporaryController = null;
        npc.Halt();

        bool atOrigin = Vector2.Distance(npc.Tile, a.OriginTile.ToVector2()) < 0.5f;
        if (atOrigin)
        {
            npc.faceDirection(a.OriginFacing);
            this.Finish(a);
            return;
        }

        if (!this.Config.ReturnToPlace)
        {
            this.Finish(a);
            return;
        }

        a.Phase = ApproachPhase.Returning;
        a.PhaseStartedUtc = DateTime.UtcNow;
        a.ReturnAttempts = 1;
        this.Log($"{npc.Name} walks back to tile {a.OriginTile.X},{a.OriginTile.Y}.");

        if (!this.TryReturnPath(a))
        {
            this.SnapBack(a);
            this.Finish(a);
        }
    }

    private bool TryReturnPath(ActiveApproach a)
    {
        NPC npc = a.Npc;
        try
        {
            var controller = new PathFindController(npc, npc.currentLocation, a.OriginTile, a.OriginFacing);
            var path = controller.pathToEndPoint;
            if (path != null && path.Count > 0)
            {
                npc.temporaryController = controller;
                npc.speed = Math.Max(a.OriginalSpeed, this.Config.ReturnSpeed);
                return true;
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Return pathfinding threw for {npc.Name}: {ex.Message}", LogLevel.Trace);
        }
        return false;
    }

    private void TickReturning(ActiveApproach a)
    {
        NPC npc = a.Npc;

        // A schedule stop started on the way back: the schedule owns the NPC from here.
        if (npc.controller != null)
        {
            this.Log($"{npc.Name}: a schedule stop started on the way back. Handing the NPC back.");
            this.Finish(a, halt: false);
            return;
        }

        if (npc.temporaryController != null)
        {
            if ((DateTime.UtcNow - a.PhaseStartedUtc).TotalSeconds > this.Config.ReturnTimeoutSeconds)
            {
                this.Log($"{npc.Name} took too long to walk back, placing it on its tile.");
                this.SnapBack(a);
                this.Finish(a);
            }
            return;
        }

        // The path ended. Close enough to the spot?
        if (Vector2.Distance(npc.Tile, a.OriginTile.ToVector2()) <= 1.0f)
        {
            this.SnapBack(a); // exact tile and facing, at most a one-tile correction
            this.Finish(a);
            return;
        }

        a.ReturnAttempts++;
        if (a.ReturnAttempts > 3 || !this.TryReturnPath(a))
        {
            this.SnapBack(a);
            this.Finish(a);
        }
    }

    /// <summary>Place the NPC exactly on the tile and facing it had before the approach.</summary>
    private void SnapBack(ActiveApproach a)
    {
        NPC npc = a.Npc;
        if (npc.currentLocation != a.OriginLocation)
            return;

        npc.Position = new Vector2(a.OriginTile.X * Game1.tileSize, a.OriginTile.Y * Game1.tileSize);
        npc.faceDirection(a.OriginFacing);
        this.Log($"{npc.Name} placed back on tile {a.OriginTile.X},{a.OriginTile.Y}.");
    }

    /*********
    ** Ending
    *********/
    private void Finish(ActiveApproach a, bool halt = true)
    {
        if (this.Active == a)
            this.Active = null;

        a.Npc.temporaryController = null;
        if (halt)
            a.Npc.Halt();
        a.Npc.speed = a.OriginalSpeed;

        if (a.Delivered)
            this.NextAllowedUtc = DateTime.UtcNow.AddSeconds(this.Config.MinSecondsBetweenApproaches);
    }

    /// <summary>Drop our state without touching the NPC (new day: the game already reset it).</summary>
    private void CancelSilently()
    {
        ActiveApproach? a = this.Active;
        if (a != null)
            this.Finish(a, halt: false);
    }

    /// <summary>The player left the room: put the NPC back on its tile, nobody sees it.</summary>
    private void CancelPlayerLeft()
    {
        ActiveApproach? a = this.Active;
        if (a == null)
            return;

        this.SnapBack(a);
        this.Finish(a);
    }

    /*********
    ** Rule checks
    *********/
    private static bool ConditionsMet(ApproachRule rule, NPC npc)
    {
        if (string.IsNullOrWhiteSpace(rule.Conditions))
            return true;
        string query = rule.Conditions.Replace("{{NpcName}}", npc.Name).Replace("{NpcName}", npc.Name);
        return GameStateQuery.CheckConditions(query, npc.currentLocation, Game1.player);
    }

    private static bool LocationAllowed(ApproachRule rule, GameLocation location)
    {
        if (rule.ExcludeLocations?.Any(t => LocationMatches(t, location)) == true)
            return false;
        if (rule.Locations is { Count: > 0 } && !rule.Locations.Any(t => LocationMatches(t, location)))
            return false;
        return true;
    }

    private static bool LocationMatches(string token, GameLocation location)
    {
        if (token.Equals("Outdoors", StringComparison.OrdinalIgnoreCase))
            return location.IsOutdoors;
        if (token.Equals("Indoors", StringComparison.OrdinalIgnoreCase))
            return !location.IsOutdoors;
        return token.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);
    }

    private static string CooldownKey(string ruleId, NPC npc) => $"{ModDataPrefix}{ruleId}/{npc.Name}";

    private bool OnCooldown(string ruleId, ApproachRule rule, NPC npc)
    {
        if (rule.Cooldown.Equals("None", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!Game1.player.modData.TryGetValue(CooldownKey(ruleId, npc), out string? stored))
            return false;
        if (rule.Cooldown.Equals("Ever", StringComparison.OrdinalIgnoreCase))
            return true;
        return stored == Game1.Date.TotalDays.ToString();
    }

    private void MarkDone(string ruleId, ApproachRule rule, NPC npc)
    {
        if (ruleId == "debug" || rule.Cooldown.Equals("None", StringComparison.OrdinalIgnoreCase))
            return;
        Game1.player.modData[CooldownKey(ruleId, npc)] = Game1.Date.TotalDays.ToString();
    }

    /*********
    ** Helpers
    *********/
    private string Pick(List<string> lines)
    {
        return lines[this.Rng.Next(lines.Count)].Replace("@", Game1.player.Name);
    }

    private static void ShowEmote(NPC npc, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        int id;
        switch (name.Trim().ToLowerInvariant())
        {
            case "question": id = 8; break;
            case "angry": id = 12; break;
            case "exclamation": id = 16; break;
            case "heart": id = 20; break;
            case "sleep": id = 24; break;
            case "sad": id = 28; break;
            case "happy": id = 32; break;
            case "music": id = 56; break;
            case "blush": id = 60; break;
            default:
                if (!int.TryParse(name, out id))
                    return;
                break;
        }

        npc.doEmote(id);
    }

    private void Log(string message)
    {
        if (this.Config.Verbose)
            this.Monitor.Log(message, LogLevel.Debug);
    }

    /*********
    ** Console commands
    *********/
    private void CmdApproach(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        if (args.Length < 1)
        {
            this.Monitor.Log("Usage: vn_approach <npc> [openMenu true|false] [force]", LogLevel.Info);
            return;
        }

        NPC? npc = Game1.getCharacterFromName(args[0]);
        if (npc == null || npc.currentLocation != Game1.currentLocation)
        {
            this.Monitor.Log($"'{args[0]}' isn't in your current location.", LogLevel.Warn);
            return;
        }

        bool openMenu = true;
        if (args.Length > 1 && bool.TryParse(args[1], out bool parsed))
            openMenu = parsed;
        bool force = args.Length > 2 && args[2].Equals("force", StringComparison.OrdinalIgnoreCase);

        // Cutting a schedule walk in half is exactly what breaks schedules, so only do it when asked.
        if (!force && (npc.controller != null || npc.temporaryController != null))
        {
            this.Monitor.Log($"{npc.Name} is walking a schedule path right now. Wait until they stand still, or add 'force' (this can break their schedule for the day).", LogLevel.Warn);
            return;
        }

        var rule = new ApproachRule
        {
            Required = true,
            OpenMenu = openMenu,
            StartLines = new List<string> { "@! Wait up!" }
        };

        this.CancelSilently();
        if (force)
        {
            npc.controller = null;
            npc.temporaryController = null;
        }
        if (!this.BeginApproach("debug", rule, npc))
            this.Monitor.Log($"{npc.Name} couldn't find a route to you.", LogLevel.Warn);
    }

    private void CmdRules(string command, string[] args)
    {
        if (!Context.IsWorldReady || Game1.currentLocation == null)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }

        Dictionary<string, ApproachRule> rules = this.LoadRules();
        this.Monitor.Log($"{rules.Count} rule(s) loaded.", LogLevel.Info);

        foreach (NPC npc in Game1.currentLocation.characters.ToList())
        {
            foreach (var pair in rules)
            {
                if (!pair.Value.AppliesTo(npc.Name))
                    continue;

                string verdict = !LocationAllowed(pair.Value, Game1.currentLocation) ? "location not allowed"
                    : this.OnCooldown(pair.Key, pair.Value, npc) ? "on cooldown"
                    : !ConditionsMet(pair.Value, npc) ? "conditions false"
                    : this.ScheduleTooClose(npc) ? "waiting: next schedule stop is too close"
                    : pair.Value.Required ? "READY (required)"
                    : "ready (optional, rolled by chance)";

                this.Monitor.Log($"{npc.Name} / {pair.Key}: {verdict}", LogLevel.Info);
            }
        }
    }
}
