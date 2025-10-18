using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Doom;
public abstract class SharedXenoDoomSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcAction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] protected readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly RMCDazedSystem _daze = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDoomComponent, XenoDoomActionEvent>(OnXenoDoomAction);

        SubscribeLocalEvent<LightDoomedComponent, ComponentStartup>(OnDoomedLightAdded);
        SubscribeLocalEvent<LightDoomedComponent, ComponentShutdown>(OnDoomedLightRemoved);
        SubscribeLocalEvent<LightDoomedComponent, AttemptPointLightToggleEvent>(OnDoomedLightAttemptToggle);
        SubscribeLocalEvent<LightDoomedComponent, PointLightToggleEvent>(OnDoomedLightToggle);
        SubscribeLocalEvent<LightDoomedComponent, ItemToggleActivateAttemptEvent>(OnDoomedLightItemToggle);

        SubscribeLocalEvent<MobDoomedComponent, ComponentStartup>(OnDoomedAdded);
        SubscribeLocalEvent<MobDoomedComponent, ComponentShutdown>(OnDoomedRemoved);
    }

    protected virtual void OnDoomedAdded(Entity<MobDoomedComponent> ent, ref ComponentStartup args)
    {

    }

    protected virtual void OnDoomedRemoved(Entity<MobDoomedComponent> ent, ref ComponentShutdown args)
    {

    }

    protected virtual void OnDoomedLightAdded(Entity<LightDoomedComponent> ent, ref ComponentStartup args)
    {
        if (!_pointLight.TryGetLight(ent, out var light))
        {
            RemCompDeferred<LightDoomedComponent>(ent);
            return;
        }

        _pointLight.SetEnabled(ent.Owner, false);
        ent.Comp.EndsAt = _timing.CurTime + ent.Comp.Duration;
    }

    private void OnDoomedLightRemoved(Entity<LightDoomedComponent> ent, ref ComponentShutdown args)
    {
        if (!_pointLight.TryGetLight(ent, out var light))
            return;

        _pointLight.SetEnabled(ent.Owner, ent.Comp.WasEnabled);
    }

    private void OnDoomedLightItemToggle(Entity<LightDoomedComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!HasComp<ItemTogglePointLightComponent>(ent))
            return;

        args.Popup = Loc.GetString("rmc-doomed-fail");
        args.Cancelled = true;
    }

    //Cannot turn on a doomed light
    //Tries to keep track of when something is switched on/off but doesn't do it perfectly
    //Oh well
    private void OnDoomedLightAttemptToggle(Entity<LightDoomedComponent> ent, ref AttemptPointLightToggleEvent args)
    {
        if (_timing.CurTime >= ent.Comp.EndsAt)
            return;

        if (ent.Comp.DoomActivated)
            ent.Comp.WasEnabled = args.Enabled;

        if (!args.Enabled)
            return;

        args.Cancelled = true;
    }

    private void OnDoomedLightToggle(Entity<LightDoomedComponent> ent, ref PointLightToggleEvent args)
    {
        if (!args.Enabled || _timing.CurTime >= ent.Comp.EndsAt)
            return;

        //Shut it off if it turns on
        ent.Comp.DoomActivated = true;
        _pointLight.SetEnabled(ent.Owner, false);
    }

    protected virtual void OnXenoDoomAction(Entity<XenoDoomComponent> xeno, ref XenoDoomActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcAction.TryUseAction(args))
            return;

        args.Handled = true;

        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);
        PredictedSpawnAttachedTo(xeno.Comp.Effect, xeno.Owner.ToCoordinates());
        PredictedSpawnAtPosition(xeno.Comp.Smoke, xeno.Owner.ToCoordinates());

        _mobs.Clear();

        _entityLookup.GetEntitiesInRange(Transform(xeno).Coordinates, xeno.Comp.Range, _mobs);

        foreach (var mob in _mobs)
        {
            if (!_examine.InRangeUnOccluded(xeno, mob))
                continue;

            if (!_xeno.CanAbilityAttackTarget(xeno, mob))
                continue;

            _status.TryAddStatusEffect<RMCBlindedComponent>(mob, "Blinded", xeno.Comp.DazeTime, true);
            _daze.TryDaze(mob, xeno.Comp.DazeTime);
            _slow.TrySuperSlowdown(mob, xeno.Comp.SlowTime, ignoreDurationModifier: true);

            if (!_solution.TryGetSolution(mob.Owner, xeno.Comp.TargetSolution, out var solEnt, out var solu))
            {
                if (solu == null || solEnt == null)
                    return;

                foreach (var chemical in solu.GetReagentPrototypes(_prototypeManager).Keys)
                {
                    _solution.RemoveReagent(solEnt.Value, chemical.ID, xeno.Comp.RemovalPerReagent);
                }
            }

            _cameraShake.ShakeCamera(mob, 6, xeno.Comp.CameraShakeStrength);

            var doom = EnsureComp<MobDoomedComponent>(mob);
            doom.EndsAt = _timing.CurTime + xeno.Comp.OverlayTime;
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<LightDoomedComponent>();

        while (query.MoveNext(out var uid, out var doomed))
        {
            if (doomed.EndsAt != null && time < doomed.EndsAt)
                continue;

            RemCompDeferred<LightDoomedComponent>(uid);
        }

        var queryWait = EntityQueryEnumerator<WaitingDoomComponent>();

        while (queryWait.MoveNext(out var uid, out var wait))
        {
            if (time < wait.DoomAt)
                continue;

            EnsureComp<LightDoomedComponent>(uid, out var doom);

            RemCompDeferred<WaitingDoomComponent>(uid);
        }

        var queryDoom = EntityQueryEnumerator<MobDoomedComponent>();

        while (queryDoom.MoveNext(out var uid, out var doom))
        {
            if (doom.EndsAt != null && time < doom.EndsAt)
                continue;

            RemCompDeferred<MobDoomedComponent>(uid);
        }
    }
}
