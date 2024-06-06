using System.Numerics;
using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Actions;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Standing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Screech;

public sealed class XenoScreechSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoScreechComponent, XenoScreechActionEvent>(OnXenoScreechAction);
    }

    private readonly HashSet<Entity<MarineComponent>> _receivers = new();

    private void OnXenoScreechAction(Entity<XenoScreechComponent> xeno, ref XenoScreechActionEvent args)
    {
        var ev = new XenoScreechAttemptEvent();
        RaiseLocalEvent(xeno, ref ev);

        if (ev.Cancelled)
            return;

        args.Handled = true;

        if (!TryComp(xeno, out TransformComponent? xform) ||
            _mobState.IsDead(xeno))
        {
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
        {
            return;
        }

        _receivers.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.StanRange, _receivers);

        foreach (var receiver in _receivers)
        {
            if (_mobState.IsDead(receiver))
                continue;
            if (TryComp(xeno, out XenoComponent? xenoComp) &&
                TryComp(receiver, out XenoComponent? targetXeno) &&
                xenoComp.Hive == targetXeno.Hive)
            {
                continue;
            }

            _stun.TryStun(receiver, xeno.Comp.StunTime, false);
        }

        _receivers.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParalyzeRange, _receivers);

        foreach (var receiver in _receivers)
        {
            if (_mobState.IsDead(receiver))
                continue;
            if (TryComp(xeno, out XenoComponent? xenoComp) &&
                TryComp(receiver, out XenoComponent? targetXeno) &&
                xenoComp.Hive == targetXeno.Hive)
            {
                continue;
            }

            _stun.TryParalyze(receiver, xeno.Comp.StunTime, true);
        }

        if (_net.IsClient)
        {
            return;
        }

        _audio.PlayPvs(xeno.Comp.Sound, xeno);
        SpawnAttachedTo(xeno.Comp.Effect, xeno.Owner.ToCoordinates());
    }
}
