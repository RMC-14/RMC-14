using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Shared._RMC14.Xenonids.Screech;

public sealed class XenoScreechSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedDeafnessSystem _deaf = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly SharedRMCLagCompensationSystem _rmcLagCompensation = default!;

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();
    private readonly HashSet<Entity<MobStateComponent>> _closeMobs = new();
    private readonly HashSet<EntityUid> _closeStunned = new();
    private readonly HashSet<Entity<XenoParasiteComponent>> _parasites = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoScreechComponent, XenoScreechActionEvent>(OnXenoScreechAction);
    }

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

        var session = CompOrNull<ActorComponent>(xeno)?.PlayerSession;

        _closeMobs.Clear();
        _closeStunned.Clear();
        // Range widened by lagcomp margin to include entities that have moved.
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParalyzeRange + xeno.Comp.LagCompensationLookupMargin, _closeMobs);

        foreach (var receiver in _closeMobs)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, receiver))
                continue;

            if (!Stun(xeno, receiver, xeno.Comp.ParalyzeTime, false, xeno.Comp.ParalyzeRange, session))
                continue;

            // Track who was actually stunned here so we don't skip over
            // targets beyond the paralyze range but still within stun range
            _closeStunned.Add(receiver);
            _cameraShake.ShakeCamera(receiver, xeno.Comp.CloseScreenShakeShakes, xeno.Comp.CloseScreenShakeStrength);
            Deafen(xeno, receiver, xeno.Comp.CloseDeafTime, session);
        }

        _mobs.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.StunRange + xeno.Comp.LagCompensationLookupMargin, _mobs);

        foreach (var receiver in _mobs)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, receiver))
                continue;

            if (_closeStunned.Contains(receiver))
                continue;

            if (!Stun(xeno, receiver, xeno.Comp.StunTime, true, xeno.Comp.StunRange, session))
                continue;

            _cameraShake.ShakeCamera(receiver, xeno.Comp.FarScreenShakeShakes, xeno.Comp.FarScreenShakeStrength);
            Deafen(xeno, receiver, xeno.Comp.FarDeafTime, session);
        }

        _parasites.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParasiteStunRange + xeno.Comp.LagCompensationLookupMargin, _parasites);

        foreach (var receiver in _parasites)
        {
            if (!Stun(xeno, receiver, xeno.Comp.ParasiteStunTime, true, xeno.Comp.ParasiteStunRange, session, false))
                continue;

            _cameraShake.ShakeCamera(receiver, xeno.Comp.CloseScreenShakeShakes, xeno.Comp.CloseScreenShakeStrength);
        }

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, xeno.Owner.ToCoordinates());
    }

    private bool Stun(EntityUid xeno, EntityUid receiver, TimeSpan time, bool stun, float range, ICommonSession? session, bool occlusionCheck = true)
    {
        if (_mobState.IsDead(receiver))
            return false;

        // Non-expanded range check against the target's lag-compensated position
        if (!_rmcLagCompensation.IsWithinMargin(xeno, receiver, session, range))
            return false;

        // Check line of sight against the target's lag-compensated position
        if (occlusionCheck && !_examineSystem.InRangeUnOccluded(xeno, _rmcLagCompensation.GetCoordinates(receiver, session)))
            return false;

        if (stun)
            return _stun.TryStun(receiver, time, false);

        return _stun.TryParalyze(receiver, time, false);
    }

    private void Deafen(EntityUid xeno, EntityUid receiver, TimeSpan time, ICommonSession? session)
    {
        if (_mobState.IsDead(receiver))
            return;

        if (!_examineSystem.InRangeUnOccluded(xeno, _rmcLagCompensation.GetCoordinates(receiver, session)))
            return;

        _deaf.TryDeafen(receiver, time, false);
    }
}
