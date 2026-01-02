using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Finesse;
using Content.Shared._RMC14.Xenonids.Sweep;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.TailTrip;

public sealed class XenoTailTripSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RMCDazedSystem _daze = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoTailTripComponent, XenoTailTripActionEvent>(OnXenoTailTripAction);
    }

    private void OnXenoTailTripAction(Entity<XenoTailTripComponent> xeno, ref XenoTailTripActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.TailEffect, args.Target.ToCoordinates());

        EnsureComp<XenoSweepingComponent>(xeno);
        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

        if (HasComp<XenoMarkedComponent>(args.Target))
        {
            if (!_size.TryGetSize(args.Target, out var size) || size < RMCSizes.Big)
                _stun.TryParalyze(args.Target, xeno.Comp.MarkedStunTime, true);

            _daze.TryDaze(args.Target, xeno.Comp.MarkedDazeTime, true, stutter: true);
            RemCompDeferred<XenoMarkedComponent>(args.Target);
        }
        else
        {
            if (!_size.TryGetSize(args.Target, out var size) || size < RMCSizes.Big)
                _stun.TryParalyze(args.Target, xeno.Comp.StunTime, true);
            _slow.TrySlowdown(args.Target, xeno.Comp.SlowTime, ignoreDurationModifier: true);
        }
    }
}
