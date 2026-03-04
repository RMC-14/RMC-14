using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Emplacements;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;
using Content.Shared._RMC14.Xenonids.DeployTraps;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.AcidMine;

public sealed class XenoAcidMineSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _sharedMap = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamage = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAcidMineComponent, XenoAcidMineActionEvent>(OnXenoAcidMineAction);
        SubscribeLocalEvent<XenoAcidMineComponent, XenoAcidMineDoAfter>(OnAcidMineDoAfter);
    }

    private void OnXenoAcidMineAction(Entity<XenoAcidMineComponent> xeno, ref XenoAcidMineActionEvent args)
    {
        args.Handled = true;

        if (!_rmcActions.TryUseAction(args))
            return;

        if (_transform.GetGrid(args.Target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        if (!_examine.InRangeUnOccluded(xeno.Owner, args.Target, xeno.Comp.Range))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-acid-mine-see-fail"), xeno, xeno);
            return;
        }

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        var coords = args.Target;

        var popupSelf = Loc.GetString("rmc-xeno-acid-mine-self");
        var popupOthers = Loc.GetString("rmc-xeno-acid-mine-others", ("xeno", xeno));
        _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

        var explodingTiles = _sharedMap.GetTilesIntersecting(
            gridId,
            grid,
            Box2.CenteredAround(coords.Position,
                new(xeno.Comp.AcidMineRadius * 2,
                    xeno.Comp.AcidMineRadius * 2)));

        //spawn telegraphs
        foreach (var tile in explodingTiles)
        {
            if (!_interaction.InRangeUnobstructed(xeno.Owner, _turf.GetTileCenter(tile), xeno.Comp.Range + 0.5f))
                continue;
            PredictedSpawnAtPosition(xeno.Comp.TelegraphEffect, _turf.GetTileCenter(tile));
        }

        var ev = new XenoAcidMineDoAfter(GetNetCoordinates(args.Target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno)
            { BreakOnMove = false, RootEntity = false };
        if (_doAfter.TryStartDoAfter(doAfter, out var id))
        {
            xeno.Comp.AcidMineDoAfter = id;
        }
    }

    private void OnAcidMineDoAfter(Entity<XenoAcidMineComponent> xeno, ref XenoAcidMineDoAfter args)
    {
        if (args.Cancelled)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, xeno.Comp.PlasmaCost))
            return;

        var coords = GetCoordinates(args.Coordinates);

        if (_transform.GetGrid(coords) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        var explodingTiles = _sharedMap.GetTilesIntersecting(
            gridId,
            grid,
            Box2.CenteredAround(coords.Position,
                new(xeno.Comp.AcidMineRadius * 2,
                    xeno.Comp.AcidMineRadius * 2)));

        HashSet<EntityUid> hitEntities = new();
        var mapCoords = _transform.ToMapCoordinates(coords);
        _lookup.GetEntitiesIntersecting(
            mapCoords.MapId,
            Box2.CenteredAround(mapCoords.Position,
                new Vector2(xeno.Comp.AcidMineRadius * 2 + 1, xeno.Comp.AcidMineRadius * 2)),
            hitEntities);

        var hits = 0;
        var trappedMobDamageMod = 1.45f;
        var empoweredMobDamageMod = 1.25f;
        var empoweredStructureDamageMod = 1.70f;

        //bootleg ability hit check that includes structures (sentries, barricades, tables, MGs and such)
        bool CanHit(EntityUid target) =>
            _xeno.CanAbilityAttackTarget(xeno, target, true, true) ||
            (!HasComp<MobStateComponent>(target) &&
             !_hive.FromSameHive(xeno.Owner, target) &&
             HasComp<DamageableComponent>(target) &&
             (Transform(target).Anchored || HasComp<BarricadeComponent>(target)));

        //sort out only valid targets
        foreach (var target in hitEntities)
        {
            if (target == xeno.Owner)
                continue;

            if (!CanHit(target))
                continue;

            _audio.PlayPredicted(xeno.Comp.SizzleSound, Transform(target).Coordinates, xeno, xeno.Comp.SizzleSound.Params.WithVolume(-10f));

            if (!_net.IsClient)
            {
                //apply damage
                if (!HasComp<MobStateComponent>(target))
                {
                    var structureDamage = xeno.Comp.Empowered
                        ? xeno.Comp.BaseDamage * empoweredStructureDamageMod
                        : xeno.Comp.BaseDamage;
                    _damage.TryChangeDamage(target, structureDamage, origin: xeno, tool: xeno);
                }
                else
                {
                    var mobDamage = xeno.Comp.BaseDamage;

                    if (xeno.Comp.Empowered)
                        mobDamage = mobDamage * empoweredMobDamageMod;

                    if (HasComp<XenoCaughtInTrapComponent>(target))
                        mobDamage = mobDamage * trappedMobDamageMod;

                    var change = _damage.TryChangeDamage(target, mobDamage, origin: xeno, tool: xeno);
                    if (change?.GetTotal() > FixedPoint2.Zero)
                    {
                        var filter = Filter.Pvs(target, entityManager: EntityManager)
                            .RemoveWhereAttachedEntity(o => o == xeno.Owner);
                        _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
                    }

                    // Apply or prolong acid effect on empowered hit
                    if (xeno.Comp.Empowered)
                    {
                        if (TryComp(target, out UserAcidedComponent? existing))
                        {
                            existing.ExpiresAt += xeno.Comp.AcidProlongDuration;
                            Dirty(target, existing);
                        }
                        else
                        {
                            var acided = EnsureComp<UserAcidedComponent>(target);
                            acided.Duration = xeno.Comp.AcidDuration;
                            acided.Damage = xeno.Comp.AcidDamage;
                            acided.ArmorPiercing = xeno.Comp.AcidArmorPiercing;
                            Dirty(target, acided);
                        }
                    }

                    hits++;
                }
            }
        }

        if (!_net.IsClient && hits > 0)
            RefreshCooldowns(xeno, hits);

        //do telegraph, SFX, VFX
        _audio.PlayPredicted(xeno.Comp.ExplosionSound, coords, xeno);
        foreach (var tile in explodingTiles)
        {
            PredictedSpawnAtPosition(xeno.Comp.SmokeEffect, _turf.GetTileCenter(tile));
        }

        //gotta remove empowered after cast.
        xeno.Comp.Empowered = false;

        foreach (var usedAction in _rmcActions.GetActionsWithEvent<XenoAcidMineActionEvent>(xeno))
        {
            _actions.SetCooldown(usedAction.AsNullable(), xeno.Comp.Cooldown);
        }
    }

    private void RefreshCooldowns(Entity<XenoAcidMineComponent> xeno, int hits)
    {
        foreach (var action in _actions.GetActions(xeno))
        {
            var actionEvent = _actions.GetEvent(action);
            if ((actionEvent is XenoDeployTrapsActionEvent)
                && action.Comp.Cooldown != null)
            {
                var cooldownEnd = action.Comp.Cooldown.Value.End - xeno.Comp.DeployTrapsCooldownReduction * hits;
                if (cooldownEnd < action.Comp.Cooldown.Value.Start)
                    _actions.ClearCooldown(action.AsNullable());
                else
                    _actions.SetCooldown(action.AsNullable(), action.Comp.Cooldown.Value.Start, cooldownEnd);
            }
        }
    }
}




