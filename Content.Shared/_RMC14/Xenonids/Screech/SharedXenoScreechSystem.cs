using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
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

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();
    private readonly HashSet<Entity<MobStateComponent>> _closeMobs = new();
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

        _closeMobs.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParalyzeRange, _closeMobs);

        foreach (var receiver in _closeMobs)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, receiver))
                continue;

            if (!Stun(xeno, receiver, xeno.Comp.ParalyzeTime, false))
                continue;

            _cameraShake.ShakeCamera(receiver, xeno.Comp.CloseScreenShakeShakes, xeno.Comp.CloseScreenShakeStrength);
            Deafen(xeno, receiver, xeno.Comp.CloseDeafTime);
        }

        _mobs.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.StunRange, _mobs);

        foreach (var receiver in _mobs)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, receiver))
                continue;

            if (_closeMobs.Contains(receiver))
                continue;

            if (!Stun(xeno, receiver, xeno.Comp.StunTime, true))
                continue;

            _cameraShake.ShakeCamera(receiver, xeno.Comp.FarScreenShakeShakes, xeno.Comp.FarScreenShakeStrength);
            Deafen(xeno, receiver, xeno.Comp.FarDeafTime);
        }

        _parasites.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParasiteStunRange, _parasites);

        foreach (var receiver in _parasites)
        {
            if (!Stun(xeno, receiver, xeno.Comp.ParasiteStunTime, true, false))
                continue;

            _cameraShake.ShakeCamera(receiver, xeno.Comp.CloseScreenShakeShakes, xeno.Comp.CloseScreenShakeStrength);
        }

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, xeno.Owner.ToCoordinates());
    }

    private bool Stun(EntityUid xeno, EntityUid receiver, TimeSpan time, bool stun, bool occlusionCheck = true)
    {
        if (_mobState.IsDead(receiver))
            return false;

        if (occlusionCheck && !_examineSystem.InRangeUnOccluded(xeno, receiver))
            return false;

        if (stun)
            return _stun.TryStun(receiver, time, false);

        return _stun.TryParalyze(receiver, time, false);
    }

    private void Deafen(EntityUid xeno, EntityUid receiver, TimeSpan time)
    {
        if (_mobState.IsDead(receiver))
            return;

        if (!_examineSystem.InRangeUnOccluded(xeno, receiver))
            return;

        _deaf.TryDeafen(receiver, time, false);
    }
}
