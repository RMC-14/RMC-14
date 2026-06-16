using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Random;
using Content.Shared.Roles;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Contains all of the selected data for a role's loadout.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class RoleLoadout : IEquatable<RoleLoadout>
{
    [DataField]
    public ProtoId<RoleLoadoutPrototype> Role;

    [DataField]
    public Dictionary<ProtoId<LoadoutGroupPrototype>, List<Loadout>> SelectedLoadouts = new();

    /// <summary>
    /// Loadout specific name.
    /// </summary>
    public string? EntityName;

    /*
     * Loadout-specific data used for validation.
     */

    public int? Points;

    public RoleLoadout(ProtoId<RoleLoadoutPrototype> role)
    {
        Role = role;
    }

    public RoleLoadout Clone()
    {
        var weh = new RoleLoadout(Role);

        foreach (var selected in SelectedLoadouts)
        {
            weh.SelectedLoadouts.Add(selected.Key, new List<Loadout>(selected.Value));
        }

        weh.EntityName = EntityName;

        weh.Points = Points;

        return weh;
    }

    /// <summary>
    /// Ensures all prototypes exist and effects can be applied.
    /// </summary>
    public void EnsureValid(HumanoidCharacterProfile profile, ICommonSession session, IDependencyCollection collection)
    {
        var groupRemove = new ValueList<string>();
        var protoManager = collection.Resolve<IPrototypeManager>();
        var configManager = collection.Resolve<IConfigurationManager>();

        if (!protoManager.TryIndex(Role, out var roleProto))
        {
            EntityName = null;
            SelectedLoadouts.Clear();
            return;
        }

        // Remove name not allowed.
        if (!roleProto.CanCustomizeName)
        {
            EntityName = null;
        }

        // RMC14 - Set loadout points and work out playtime point rewards.
        var calculatedRolePoints = CalculatePointsFromPlaytime(session);
        // End RMC14

        // Validate name length
        // TODO: Probably allow regex to be supplied?
        if (EntityName != null)
        {
            var name = EntityName.Trim();
            var maxNameLength = configManager.GetCVar(CCVars.MaxNameLength);

            if (name.Length > maxNameLength)
            {
                EntityName = name[..maxNameLength];
            }

            if (name.Length == 0)
            {
                EntityName = null;
            }
        }

        // In some instances we might not have picked up a new group for existing data.
        foreach (var groupProto in roleProto.Groups)
        {
            if (SelectedLoadouts.ContainsKey(groupProto))
                continue;

            // Data will get set below.
            SelectedLoadouts[groupProto] = new List<Loadout>();
        }

        // RMC14
        // Reset points to recalculate. 
        Points = calculatedRolePoints;
        // End RMC14

        foreach (var (group, groupLoadouts) in SelectedLoadouts)
        {
            // Check the group is even valid for this role.
            if (!roleProto.Groups.Contains(group))
            {
                groupRemove.Add(group);
                continue;
            }

            // Dump if Group doesn't exist
            if (!protoManager.TryIndex(group, out var groupProto))
            {
                groupRemove.Add(group);
                continue;
            }

            var loadouts = groupLoadouts[..Math.Min(groupLoadouts.Count, groupProto.MaxLimit)];

            // Validate first
            for (var i = loadouts.Count - 1; i >= 0; i--)
            {
                var loadout = loadouts[i];

                // Old prototype or otherwise invalid.
                if (!protoManager.TryIndex(loadout.Prototype, out var loadoutProto))
                {
                    loadouts.RemoveAt(i);
                    continue;
                }

                // Malicious client maybe, check the group even has it.
                if (!groupProto.Loadouts.Contains(loadout.Prototype))
                {
                    loadouts.RemoveAt(i);
                    continue;
                }

                // Validate the loadout can be applied (e.g. points).
                if (!IsValid(profile, session, loadout.Prototype, collection, out _))
                {
                    loadouts.RemoveAt(i);
                    continue;
                }

                Apply(loadoutProto);
            }

            // Apply defaults if required
            // Technically it's possible for someone to game themselves into loadouts they shouldn't have
            // If you put invalid ones first but that's your fault for not using sensible defaults
            if (loadouts.Count < groupProto.MinLimit)
            {
                foreach (var protoId in groupProto.Loadouts)
                {
                    if (loadouts.Count >= groupProto.MinLimit)
                        break;

                    if (!protoManager.TryIndex(protoId, out var loadoutProto))
                        continue;

                    var defaultLoadout = new Loadout()
                    {
                        Prototype = loadoutProto.ID,
                    };

                    if (loadouts.Contains(defaultLoadout))
                        continue;

                    // Not valid so don't default to it anyway.
                    if (!IsValid(profile, session, defaultLoadout.Prototype, collection, out _))
                        continue;

                    loadouts.Add(defaultLoadout);
                    Apply(loadoutProto);
                }
            }

            SelectedLoadouts[group] = loadouts;
        }

        foreach (var value in groupRemove)
        {
            SelectedLoadouts.Remove(value);
        }
    }

    private void Apply(LoadoutPrototype loadoutProto)
    {
        foreach (var effect in loadoutProto.Effects)
        {
            effect.Apply(this);
        }

        if (loadoutProto.Cost != null && Points != null)
            Points -= loadoutProto.Cost;
    }

    /// <summary>
    /// Resets the selected loadouts to default if no data is present.
    /// </summary>
    public void SetDefault(HumanoidCharacterProfile? profile, ICommonSession? session, IPrototypeManager protoManager, bool force = false)
    {
        if (profile == null)
            return;

        if (force)
            SelectedLoadouts.Clear();

        var collection = IoCManager.Instance!;
        var roleProto = protoManager.Index(Role);

        for (var i = roleProto.Groups.Count - 1; i >= 0; i--)
        {
            var group = roleProto.Groups[i];

            if (!protoManager.TryIndex(group, out var groupProto))
                continue;

            if (SelectedLoadouts.ContainsKey(group))
                continue;

            var loadouts = new List<Loadout>();
            SelectedLoadouts[group] = loadouts;

            // RMC14 - Set loadout points and work out playtime point rewards.
            Points = CalculatePointsFromPlaytime(session);
            // End RMC14

            if (groupProto.MinLimit > 0)
            {
                // Apply any loadouts we can.
                foreach (var protoId in groupProto.Loadouts)
                {
                    // Reached the limit, time to stop
                    if (loadouts.Count >= groupProto.MinLimit)
                        break;

                    if (!protoManager.TryIndex(protoId, out var loadoutProto))
                        continue;

                    var defaultLoadout = new Loadout()
                    {
                        Prototype = loadoutProto.ID,
                    };

                    // Not valid so don't default to it anyway.
                    if (!IsValid(profile, session, defaultLoadout.Prototype, collection, out _))
                        continue;

                    loadouts.Add(defaultLoadout);
                    Apply(loadoutProto);
                }
            }
        }
    }

    /// <summary>
    /// Returns whether a loadout is valid or not.
    /// </summary>
    public bool IsValid(HumanoidCharacterProfile profile, ICommonSession? session, ProtoId<LoadoutPrototype> loadout, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        var protoManager = collection.Resolve<IPrototypeManager>();

        if (!protoManager.TryIndex(loadout, out var loadoutProto))
        {
            // Uhh
            reason = FormattedMessage.FromMarkupOrThrow("");
            return false;
        }

        if (!protoManager.HasIndex(Role))
        {
            reason = FormattedMessage.FromUnformatted("loadouts-prototype-missing");
            return false;
        }

        if (loadoutProto.Cost != null && Points != null)
        {
            if (Points < loadoutProto.Cost)
            {
                reason = FormattedMessage.FromUnformatted(Loc.GetString("loadout-group-points-insufficient"));
                return false;
            }
        }

        var valid = true;

        foreach (var effect in loadoutProto.Effects)
        {
            valid = valid && effect.Validate(profile, this, session, collection, out reason);
        }

        return valid;
    }

    /// <summary>
    /// Applies the specified loadout to this group.
    /// </summary>
    public bool AddLoadout(ProtoId<LoadoutGroupPrototype> selectedGroup, ProtoId<LoadoutPrototype> selectedLoadout, IPrototypeManager protoManager)
    {
        // RMC14
        if (!SelectedLoadouts.TryGetValue(selectedGroup, out var groupLoadouts))
        {
            groupLoadouts = new List<Loadout>();
            SelectedLoadouts[selectedGroup] = groupLoadouts;
        }
        // End RMC14

        // Need to unselect existing ones if we're at or above limit
        var limit = Math.Max(0, groupLoadouts.Count + 1 - protoManager.Index(selectedGroup).MaxLimit);

        for (var i = 0; i < groupLoadouts.Count; i++)
        {
            var loadout = groupLoadouts[i];

            if (loadout.Prototype != selectedLoadout)
            {
                // Remove any other loadouts that might push it above the limit.
                if (limit > 0)
                {
                    // RMC14
                    // Refund points for the removed loadout selection immediately so UI can reflect cost changes.
                    if (Points != null)
                    {
                        if (protoManager.TryIndex(loadout.Prototype, out var removedProto) && removedProto.Cost != null)
                        {
                            Points += removedProto.Cost.Value;
                        }
                    }
                    // End RMC14

                    limit--;
                    groupLoadouts.RemoveAt(i);
                    i--;
                }

                continue;
            }
            return false;
        }

        groupLoadouts.Add(new Loadout()
        {
            Prototype = selectedLoadout,
        });

        return true;
    }

    /// <summary>
    /// Removed the specified loadout from this group.
    /// </summary>
    public bool RemoveLoadout(ProtoId<LoadoutGroupPrototype> selectedGroup, ProtoId<LoadoutPrototype> selectedLoadout, IPrototypeManager protoManager)
    {
        // Although this may bring us below minimum we'll let EnsureValid handle it.

        var groupLoadouts = SelectedLoadouts[selectedGroup];

        for (var i = 0; i < groupLoadouts.Count; i++)
        {
            var loadout = groupLoadouts[i];

            if (loadout.Prototype != selectedLoadout)
                continue;

            groupLoadouts.RemoveAt(i);
            return true;
        }

        return false;
    }

    public bool Equals(RoleLoadout? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        if (!Role.Equals(other.Role) ||
            SelectedLoadouts.Count != other.SelectedLoadouts.Count ||
            Points != other.Points ||
            EntityName != other.EntityName)
        {
            return false;
        }

        // Tried using SequenceEqual but it stinky so.
        foreach (var (key, value) in SelectedLoadouts)
        {
            if (!other.SelectedLoadouts.TryGetValue(key, out var otherValue) ||
                !otherValue.SequenceEqual(value))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is RoleLoadout other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Role, SelectedLoadouts, Points);
    }

    // RMC14
    // Public helper so client code can reuse the same playtime -> points calculation
    public static int CalculatePointsFromPlaytime(ICommonSession? session)
    {
        // If we don't have a session or can't resolve playtimes, treat as 0 hours
        try
        {
            if (session == null)
                return CalculatePointsFromHours(0);

            var playtimeManager = IoCManager.Resolve<ISharedPlaytimeManager>();
            var playtimes = playtimeManager.GetPlayTimes(session);

            if (playtimes == null || playtimes.Count == 0)
                return CalculatePointsFromHours(0);

            var overallKey = PlayTimeTrackingShared.TrackerOverall.ToString();

            var totalTicks = playtimes
                .Where(kvp => kvp.Key != overallKey)
                .Sum(kvp => kvp.Value.Ticks);

            var playtimeHours = (int)new TimeSpan(totalTicks).TotalHours;

            return CalculatePointsFromHours(playtimeHours);
        }
        catch
        {
            return CalculatePointsFromHours(0);
        }
    }

    private static int CalculatePointsFromHours(int playtimeHours)
    {
        // Every 100 hours = 1 additional point + base amount 7, capped at 20
        int points = (int)(playtimeHours / 100) + 7;
        if (points > 20)
            points = 20;

        return points;
    }
    // End RMC14
}
