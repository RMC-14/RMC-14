using Content.Shared._RMC14.Body.Zones;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Shared._RMC14.Body.Limbs;

public sealed class RMCLimbHealthSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    /// <summary>
    ///     the damage on a limb, eyes and mouth count as the head
    /// </summary>
    public RMCLimb GetLimb(Entity<RMCLimbHealthComponent?> ent, RMCTargetZone zone)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return default;

        return ent.Comp.Limbs.GetValueOrDefault(zone.ToDamageZone());
    }

    public bool TryGetDefinition(Entity<RMCLimbHealthComponent?> ent, RMCTargetZone zone, out RMCLimbDefinition definition)
    {
        definition = default;
        return Resolve(ent, ref ent.Comp, false) &&
               ent.Comp.Definitions.TryGetValue(zone.ToDamageZone(), out definition);
    }

    /// <summary>
    ///     the limb this one hangs off, null for the chest
    /// </summary>
    public RMCTargetZone? GetParent(Entity<RMCLimbHealthComponent?> ent, RMCTargetZone zone)
    {
        return TryGetDefinition(ent, zone, out var definition) ? definition.Parent : null;
    }

    /// <summary>
    ///     the limbs that hang off this one
    /// </summary>
    public void GetChildren(Entity<RMCLimbHealthComponent?> ent, RMCTargetZone zone, List<RMCTargetZone> children)
    {
        children.Clear();
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        zone = zone.ToDamageZone();
        foreach (var (child, definition) in ent.Comp.Definitions)
        {
            if (definition.Parent == zone)
                children.Add(child);
        }
    }

    /// <summary>
    ///     whether the mob still has the body part for this limb, a missing part means a missing limb
    /// </summary>
    public bool HasLimb(Entity<RMCLimbHealthComponent?, BodyComponent?> ent, RMCTargetZone zone)
    {
        if (!Resolve(ent, ref ent.Comp1, false) ||
            !Resolve(ent, ref ent.Comp2, false))
        {
            return false;
        }

        zone = zone.ToDamageZone();
        if (!ent.Comp1.Definitions.ContainsKey(zone))
            return false;

        var (type, symmetry) = zone.ToBodyPart();
        foreach (var part in _body.GetBodyChildrenOfType(ent, type, ent.Comp2))
        {
            if (part.Component.Symmetry == symmetry)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     every limb the mob still has
    /// </summary>
    public void GetPresentLimbs(Entity<RMCLimbHealthComponent?, BodyComponent?> ent, List<RMCTargetZone> limbs)
    {
        limbs.Clear();
        if (!Resolve(ent, ref ent.Comp1, false) ||
            !Resolve(ent, ref ent.Comp2, false))
        {
            return;
        }

        foreach (var part in _body.GetBodyChildren(ent, ent.Comp2))
        {
            if (part.Component.PartType.ToZone(part.Component.Symmetry) is not { } zone)
                continue;

            if (ent.Comp1.Definitions.ContainsKey(zone))
                limbs.Add(zone);

            // TODO RMC14 abdomen body part, the torso carries the abdomen until it gets its own part
            if (zone == RMCTargetZone.Chest && ent.Comp1.Definitions.ContainsKey(RMCTargetZone.Abdomen))
                limbs.Add(RMCTargetZone.Abdomen);
        }
    }
}
