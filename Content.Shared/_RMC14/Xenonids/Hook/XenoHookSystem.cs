using Content.Shared._RMC14.Line;
using Content.Shared.Throwing;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Hook;

public sealed partial class XenoHookSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly LineSystem _line = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoHookComponent, MoveEvent>(OnHookSourceMove);
        SubscribeLocalEvent<XenoHookComponent, EntityTerminatingEvent>(OnHookDelete);
        SubscribeLocalEvent<XenoHookedComponent, MoveEvent>(OnHookedMove);
        SubscribeLocalEvent<XenoHookedComponent, StopThrowEvent>(OnHookedStop);
        SubscribeLocalEvent<XenoHookedComponent, ComponentShutdown>(OnHookedRemoved);
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
                continue;
            }

            UpdateTail((hooked, hookComp));
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

    private void OnHookedMove(Entity<XenoHookedComponent> ent, ref MoveEvent args)
    {
        UpdateTail(ent);
    }

    private void OnHookedStop(Entity<XenoHookedComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred<XenoHookedComponent>(ent);
    }

    private void OnHookedRemoved(Entity<XenoHookedComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<XenoHookComponent>(ent.Comp.Source, out var hookSource))
            hookSource.Hooked.Remove(ent);
        ent.Comp.StopUpdating = true;
        Dirty(ent);
        _line.DeleteBeam(ent.Comp.Tail);
        _appearance.SetData(ent, HookedVisuals.Hooked, false);
    }

    public bool TryHookTarget(Entity<XenoHookComponent> xeno, EntityUid target)
    {
        //No double hooks
        if (HasComp<XenoHookedComponent>(target))
            return false;

        var hook = EnsureComp<XenoHookedComponent>(target);

        hook.Source = xeno;
        hook.TailProto = xeno.Comp.TailProto;
        xeno.Comp.Hooked.Add(target);
        Dirty(xeno);

        _appearance.SetData(target, HookedVisuals.Hooked, true);
        UpdateTail((target, hook));

        return true;
    }

    public void UpdateTail(Entity<XenoHookedComponent> ent)
    {
        if (_net.IsClient)
            return;

        var hook = ent.Comp;

        if (hook.StopUpdating)
            return;

        if (hook.Tail.Count != 0)
            _line.DeleteBeam(hook.Tail);

        if (_line.TryCreateLine(hook.Source, ent, hook.TailProto, out var lines))
            hook.Tail = lines;
    }
}
