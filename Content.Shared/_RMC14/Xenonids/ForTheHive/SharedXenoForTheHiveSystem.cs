using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Mobs.Components;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Content.Shared._RMC14.Xenonids.Acid;
using Robust.Shared.Map.Components;
using Content.Shared.Interaction;
using Content.Shared._RMC14.Xenonids.Construction;
using Robust.Shared.Map;
using Content.Shared.Movement.Systems;

namespace Content.Shared._RMC14.Xenonids.ForTheHive;

public abstract partial class SharedXenoForTheHiveSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly XenoEnergySystem _energy = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoEvolutionSystem _evolution = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedCMChatSystem _chat = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] protected readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedXenoAcidSystem _acid = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForTheHiveComponent, XenoForTheHiveActionEvent>(OnForTheHiveActivated);

        SubscribeLocalEvent<ActiveForTheHiveComponent, ComponentStartup>(OnForTheHiveAdded);
        SubscribeLocalEvent<ActiveForTheHiveComponent, ComponentShutdown>(OnForTheHiveRemoved);
        SubscribeLocalEvent<ActiveForTheHiveComponent, ComponentRemove>(OnForTheHiveGone);

        SubscribeLocalEvent<ActiveForTheHiveComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }


    private void OnForTheHiveActivated(Entity<ForTheHiveComponent> xeno, ref XenoForTheHiveActionEvent args)
    {
        args.Handled = true;
        if (_container.IsEntityInContainer(xeno))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-for-the-hive-container"), xeno, xeno, PopupType.SmallCaution);
            return;
        }

        //TODO can't activate while in containment either

        if (HasComp<ActiveForTheHiveComponent>(xeno))
        {
            if (!TryComp<XenoEnergyComponent>(xeno, out var acid))
                return;

            _energy.TryRemoveEnergy(xeno.Owner, acid.Current / 4);

            _popup.PopupClient(Loc.GetString("rmc-xeno-for-the-hive-cancel"), xeno, xeno);
            RemCompDeferred<ActiveForTheHiveComponent>(xeno);
            return;
        }

        if (!_energy.HasEnergyPopup(xeno.Owner, xeno.Comp.Minimum))
            return;

        var hive = EnsureComp<ActiveForTheHiveComponent>(xeno);
        hive.Duration = xeno.Comp.Duration;
        hive.TimeLeft = xeno.Comp.Duration;
        hive.BaseDamage = xeno.Comp.BaseDamage;
        hive.MobAcid = xeno.Comp.Acid;

        ForTheHiveShout(xeno);

        _popup.PopupClient(Loc.GetString("rmc-xeno-for-the-hive-activate"), xeno, xeno, PopupType.Medium);
    }

    protected virtual void ForTheHiveShout(EntityUid xeno)
    {
    }

    private void OnForTheHiveAdded(Entity<ActiveForTheHiveComponent> xeno, ref ComponentStartup args)
    {
        xeno.Comp.NextUpdate = _timing.CurTime + xeno.Comp.UpdateEvery;
        var ev = new ForTheHiveActivatedEvent();
        RaiseLocalEvent(xeno, ref ev);
        _pointLight.SetEnabled(xeno, true);
        _movement.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnForTheHiveRemoved(Entity<ActiveForTheHiveComponent> xeno, ref ComponentShutdown args)
    {
        var ev = new ForTheHiveCancelledEvent();
        RaiseLocalEvent(xeno, ref ev);
        _pointLight.SetEnabled(xeno, false);
    }

    private void OnForTheHiveGone(Entity<ActiveForTheHiveComponent> xeno, ref ComponentRemove args)
    {
        _movement.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnRefreshSpeed(Entity<ActiveForTheHiveComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        var multiplier = xeno.Comp.SlowDown.Float();
        args.ModifySpeed(multiplier, multiplier);
    }


    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var forTheHiveQuery = EntityQueryEnumerator<ActiveForTheHiveComponent>();

        while (forTheHiveQuery.MoveNext(out var xeno, out var active))
        {
            if (active.NextUpdate <= time && !_mob.IsDead(xeno))
            {
                if (active.TimeLeft.TotalSeconds % 2 == 0 && (active.TimeLeft - active.UpdateEvery).TotalSeconds > 0)
                {
                    var volume = -3f + (float)(active.MaxVolume * (1.0 - (active.TimeLeft.TotalSeconds / active.Duration.TotalSeconds)));

                    if (active.UseWindUpSound)
                        _audio.PlayPvs(active.WindingUpSound, xeno, AudioParams.Default.WithVolume(volume));
                    else
                        _audio.PlayPvs(active.WindingDownSound, xeno, AudioParams.Default.WithVolume(volume));

                    active.UseWindUpSound = !active.UseWindUpSound;

                }
                active.TimeLeft -= active.UpdateEvery;
                active.NextUpdate += active.UpdateEvery;

                _appearance.SetData(xeno, ForTheHiveVisuals.Time, (float) (active.TimeLeft / active.Duration));
                var popupType = PopupType.MediumCaution;

                if (active.TimeLeft / active.Duration <= 0.5f)
                    popupType = PopupType.LargeCaution;

                if (active.TimeLeft.TotalSeconds > 0)
                    _popup.PopupEntity(active.TimeLeft.TotalSeconds.ToString(), xeno, xeno, popupType);
                else
                {
                    //Kaboom
                    if (!TryComp<XenoEnergyComponent>(xeno, out var acid))
                        return;

                    var acidRange = acid.Current / active.AcidRangeRatio;
                    var burnRange = acid.Current / active.BurnRangeRatio;

                    var maxBurnDamage = acid.Current / active.BurnDamageRatio;

                    var origin = _transform.GetMoverCoordinates(xeno);

                    //Everything the rouny has in view get hit
                    //Acid = smoke & cades
                    //Burn = mobs

                    foreach (var cade in _lookup.GetEntitiesInRange<BarricadeComponent>(origin, acidRange))
                    {
                        if (!_interaction.InRangeUnobstructed(xeno, cade.Owner, acidRange, collisionMask: Physics.CollisionGroup.Impassable))
                            continue;

                        if (HasComp<DamageableCorrodingComponent>(cade))
                            continue;

                        _acid.ApplyAcid(active.Acid, cade, active.AcidDps, 0, active.AcidTime);

                    }


                    foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(origin, burnRange))
                    {
                        if (!_xeno.CanAbilityAttackTarget(xeno, mob))
                            continue;

                        //Do the acid check here
                        if (_interaction.InRangeUnobstructed(xeno, mob.Owner, acidRange, collisionMask: Physics.CollisionGroup.Impassable))
                        {
                            if (active.MobAcid is { } add)
                                EntityManager.AddComponents(mob, add);
                        }

                        if (!_interaction.InRangeUnobstructed(xeno, mob.Owner, burnRange, collisionMask: Physics.CollisionGroup.Impassable))
                            continue;

                        if (!origin.TryDistance(EntityManager, _transform.GetMoverCoordinates(mob), out var distance))
                            continue;

                        var damage = ((burnRange - distance) * maxBurnDamage) / burnRange;

                        _damage.TryChangeDamage(mob, active.BaseDamage * damage, true, origin: xeno);

                    }

                    if (_transform.GetGrid(xeno) is not { } gridId || !TryComp<MapGridComponent>(gridId, out var grid))
                        continue;

                    foreach (var turf in _map.GetTilesIntersecting(gridId, grid, Box2.CenteredAround(origin.Position, new(acidRange * 2, acidRange * 2)), false))
                    {
                        if (!_interaction.InRangeUnobstructed(_transform.ToMapCoordinates(origin), _transform.ToMapCoordinates(_turf.GetTileCenter(turf)), acidRange, collisionMask: Physics.CollisionGroup.Impassable))
                            continue;

                        if (_turf.IsTileBlocked(turf, Physics.CollisionGroup.Impassable))
                            continue;

                        var smoke = SpawnAtPosition(active.AcidSmoke, _turf.GetTileCenter(turf));
                    }

                    //TODO CM gibs the runner
                    _damage.TryChangeDamage(xeno, active.BaseDamage * 5000, true);
                    _audio.PlayPvs(active.KaboomSound, xeno);
                    RemCompDeferred<ActiveForTheHiveComponent>(xeno);

                    if (GetHiveCore(xeno, out var core))
                        ForTheHiveRespawn(xeno, active.CoreSpawnTime);
                    else
                        ForTheHiveRespawn(xeno, active.CorpseSpawnTime, true, origin);
                }
            }
        }
    }

    protected bool GetHiveCore(EntityUid xeno, out EntityUid? core)
    {
        var cores = EntityQueryEnumerator<HiveCoreComponent, HiveMemberComponent>();
        while (cores.MoveNext(out var uid, out var _, out var _))
        {
            if (!_hive.FromSameHive(xeno, uid))
                continue;

            if (_mob.IsDead(uid))
                continue;

            core = uid;
            return true;
        }

        core = null;
        return false;
    }

    protected virtual void ForTheHiveRespawn(EntityUid xeno, TimeSpan time, bool atCorpse = false, EntityCoordinates? corpse = null)
    {
    }
}
