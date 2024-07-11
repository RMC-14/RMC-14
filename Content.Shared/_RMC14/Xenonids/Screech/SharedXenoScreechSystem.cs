using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Screech;

public sealed class XenoScreechSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoScreechComponent, XenoScreechActionEvent>(OnXenoScreechAction);
    }

    private readonly HashSet<Entity<MarineComponent>> _receivers = new();
    private readonly CollisionGroup _opaqueObjectsMask = CollisionGroup.Opaque;

    private void OnXenoScreechAction(Entity<XenoScreechComponent> xeno, ref XenoScreechActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoScreechAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        if (!TryComp(xeno, out TransformComponent? xform))
            return;

        args.Handled = true;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        _receivers.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.StunRange, _receivers);

        foreach (var receiver in _receivers)
        {
            if (_mobState.IsDead(receiver))
                continue;

            if (!_interactionSystem.InRangeUnobstructed(xeno.Owner,
                    receiver.Owner,
                    xeno.Comp.StunRange,
                    collisionMask: _opaqueObjectsMask))
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

            if (!_interactionSystem.InRangeUnobstructed(xeno.Owner,
                    receiver.Owner,
                    xeno.Comp.ParalyzeRange,
                    collisionMask: _opaqueObjectsMask))
                continue;

            if (TryComp(xeno, out XenoComponent? xenoComp) &&
                TryComp(receiver, out XenoComponent? targetXeno) &&
                xenoComp.Hive == targetXeno.Hive)
            {
                continue;
            }

            _stun.TryParalyze(receiver, xeno.Comp.ParalyzeTime, true);
        }

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, xeno.Owner.ToCoordinates());
    }
}
