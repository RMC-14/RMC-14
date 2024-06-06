using System.Numerics;
using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Throwing;
using Content.Shared.DoAfter;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Gut;

public abstract class SharedXenoGutSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private EntityQuery<MarineComponent> _marineQuery;

    public override void Initialize()
    {
        base.Initialize();
        _marineQuery = GetEntityQuery<MarineComponent>();

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

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var target = args.Target;
        if (!_marineQuery.HasComponent(target))
            return;

        if (!TryComp<BodyComponent>(target, out var body))
            return;

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

        if (_net.IsClient)
            return;

        _audio.PlayPvs(xeno.Comp.Sound, xeno);
    }
}
