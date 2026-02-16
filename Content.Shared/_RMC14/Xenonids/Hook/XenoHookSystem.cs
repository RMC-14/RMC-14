using Content.Shared._RMC14.Emplacements;
using Content.Shared._RMC14.Tether;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Shared._RMC14.Xenonids.Hook;

public sealed partial class XenoHookSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoHookComponent, MoveEvent>(OnHookSourceMove);
        SubscribeLocalEvent<XenoHookComponent, EntityTerminatingEvent>(OnHookDelete);
        SubscribeLocalEvent<XenoHookedComponent, StopThrowEvent>(OnHookedStop);
        SubscribeLocalEvent<XenoHookedComponent, ComponentShutdown>(OnHookedRemoved);
        SubscribeLocalEvent<XenoHookedComponent, PreventCollideEvent>(OnHookedPreventCollide);
    }

    private void OnHookSourceMove(Entity<XenoHookComponent> xeno, ref MoveEvent args)
    {
        if (xeno.Comp.Hooked.Count == 0)
            return;

        List<EntityUid> toRemove = new();

        foreach (var hooked in xeno.Comp.Hooked)
        {
            if (!TryComp<XenoHookedComponent>(hooked, out var hookComp))
            {
                toRemove.Add(hooked);
            }
        }

        foreach (var ent in toRemove)
        {
            xeno.Comp.Hooked.Remove(ent);
        }
    }

    private void OnHookDelete(Entity<XenoHookComponent> xeno, ref EntityTerminatingEvent args)
    {
        if (xeno.Comp.Hooked.Count == 0)
            return;

        foreach (var hooked in xeno.Comp.Hooked)
        {
            RemCompDeferred<XenoHookedComponent>(hooked);
        }

        xeno.Comp.Hooked.Clear();
    }

    private void OnHookedStop(Entity<XenoHookedComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred<XenoHookedComponent>(ent);
    }

    private void OnHookedRemoved(Entity<XenoHookedComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<RMCTetherComponent>(ent);
    }

    private void OnHookedPreventCollide(Entity<XenoHookedComponent> ent, ref PreventCollideEvent args)
    {
        if (HasComp<WeaponMountComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    public bool TryHookTarget(Entity<XenoHookComponent> xeno, EntityUid target)
    {
        //No double hooks
        if (HasComp<XenoHookedComponent>(target))
            return false;

        EnsureComp<XenoHookedComponent>(target);
        xeno.Comp.Hooked.Add(target);
        Dirty(xeno);

        var tether = EnsureComp<RMCTetherComponent>(target);
        tether.TetherOrigin = xeno;
        Dirty(target, tether);

        return true;
    }
}
