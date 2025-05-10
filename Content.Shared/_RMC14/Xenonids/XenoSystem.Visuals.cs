using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Fortify;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Mobs;
using Content.Shared.Standing;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids;

public sealed partial class XenoSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private void OnVisualsMobStateChanged(Entity<XenoStateVisualsComponent> ent, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(ent, RMCXenoStateVisuals.Downed, args.NewMobState != MobState.Alive);
        _appearance.SetData(ent, RMCXenoStateVisuals.Dead, args.NewMobState == MobState.Dead);
    }

    private void OnVisualsFortified(Entity<XenoStateVisualsComponent> ent, ref XenoFortifiedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(ent, RMCXenoStateVisuals.Fortified, args.Fortified);
    }

    private void OnVisualsRest(Entity<XenoStateVisualsComponent> ent, ref XenoRestEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(ent, RMCXenoStateVisuals.Resting, args.Resting);
    }

    private void OnVisualsProne(Entity<XenoStateVisualsComponent> xeno, ref DownedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, RMCXenoStateVisuals.Downed, true);
    }

    private void OnVisualsStand(Entity<XenoStateVisualsComponent> xeno, ref StoodEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, RMCXenoStateVisuals.Downed, false);
    }

    private void OnVisualsOvipositor(Entity<XenoStateVisualsComponent> xeno, ref XenoOvipositorChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, RMCXenoStateVisuals.Ovipositor, args.Attached);
    }
}

[Serializable, NetSerializable]
public enum RMCXenoStateVisuals
{
    Resting,
    Downed,
    Fortified,
    Ovipositor,
    Dead,
}
