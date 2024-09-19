using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Standing;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Tremor;

public sealed class XenoTremorSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoTremorComponent, XenoTremorActionEvent>(OnXenoTremorAction);
    }

    private readonly HashSet<Entity<MarineComponent>> _receivers = new();

    private void OnXenoTremorAction(Entity<XenoTremorComponent> xeno, ref XenoTremorActionEvent args)
    {
        var ev = new XenoTremorAttemptEvent();
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
            return;

        _receivers.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.Range, _receivers);

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        foreach (var receiver in _receivers)
        {
            if (_mobState.IsDead(receiver))
                continue;

            _stun.TryParalyze(receiver, xeno.Comp.ParalyzeTime, true);
            if (_net.IsServer)
                SpawnAttachedTo(xeno.Comp.Effect, receiver.Owner.ToCoordinates());

        }
    }
}
