using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.DoAfter;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Gut;

public sealed class SharedXenoGutSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoGutComponent, XenoGutActionEvent>(OnXenoGutAction);
        SubscribeLocalEvent<XenoGutComponent, XenoGutDoAfterEvent>(OnXenoGutDoAfterEvent);
    }

    private void OnXenoGutAction(Entity<XenoGutComponent> xeno, ref XenoGutActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoGutAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        var target = args.Target;

        if (!TryComp<BodyComponent>(target, out var body))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var ev = new XenoGutDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.GutDelay, ev, xeno, target)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoGutDoAfterEvent(Entity<XenoGutComponent> xeno, ref XenoGutDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target != null &
            !TryComp<BodyComponent>(args.Target, out var body))
        {
            return;
        }

        if (args.Target != null)
            _bodySystem.GibBody(args.Target.Value, true, body);

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);
    }
}
