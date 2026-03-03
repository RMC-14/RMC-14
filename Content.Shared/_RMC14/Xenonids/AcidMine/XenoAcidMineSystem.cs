using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Construction.FloorResin;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.DeployTraps;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Insight;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.ResinSurge;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.MouseRotator;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using YamlDotNet.Core;

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
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;

    private EntityQuery<BarricadeComponent> _barricadeQuery;

    public override void Initialize()
    {
        _barricadeQuery = GetEntityQuery<BarricadeComponent>();

        SubscribeLocalEvent<XenoAcidMineComponent, XenoAcidMineActionEvent>(OnXenoAcidMineAction);
        SubscribeLocalEvent<XenoAcidMineComponent, XenoAcidMineDoAfter>(OnAcidMineDoAfter);
    }

    private void OnXenoAcidMineAction(Entity<XenoAcidMineComponent> xeno, ref XenoAcidMineActionEvent args)
    {

        args.Handled = true;

        // Check if target on grid
        if (_transform.GetGrid(args.Target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        if (!_examine.InRangeUnOccluded(xeno.Owner, args.Target, xeno.Comp.Range))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-acid-mine-see-fail"), xeno, xeno);
            return;
        }

        var ev = new XenoAcidMineDoAfter(GetNetCoordinates(args.Target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.Delay, ev, xeno) { BreakOnMove = true, RootEntity = true };
        if (_doAfter.TryStartDoAfter(doAfter, out var id))
        {
            xeno.Comp.AcidMineDoAfter = id;
        }
    }

    private void OnAcidMineDoAfter(Entity<XenoAcidMineComponent> xeno, ref XenoAcidMineDoAfter args)
    {
        xeno.Comp.AcidMineDoAfter = null;
        if (args.Cancelled)
            return;

        // Check if user has enough plasma
        if (!_xenoPlasma.TryRemovePlasmaPopup((xeno.Owner, null), xeno.Comp.PlasmaCost))
            return;

        var coords = GetCoordinates(args.Coordinates);

        if (_transform.GetGrid(coords) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        var popupSelf = Loc.GetString("rmc-xeno-deploy-traps-self");
        var popupOthers = Loc.GetString("rmc-xeno-deploy-traps-others", ("xeno", xeno));
        _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

        var explodingTiles = _sharedMap.GetTilesIntersecting(
            gridId,
            grid,
            Box2.CenteredAround(coords.Position,
                new(xeno.Comp.AcidMineRadius * 2,
                    xeno.Comp.AcidMineRadius * 2)));

        //total list of struck entities
        HashSet<EntityUid> hitEntities = new();
        var mapCoords = _transform.ToMapCoordinates(coords);
        _lookup.GetEntitiesIntersecting(
            mapCoords.MapId,
            Box2.CenteredAround(mapCoords.Position,
                new Vector2(xeno.Comp.AcidMineRadius * 2 + 1, xeno.Comp.AcidMineRadius * 2)),
            hitEntities);

        var cadeDamage = xeno.Comp.Empowered
            ? new DamageSpecifier(xeno.Comp.DamageToStructuresEmpowered)
            : new DamageSpecifier(xeno.Comp.DamageToStructures);

        var hits = 0;

        if (!_net.IsClient)
        {
            //sort out only valid targets
            foreach (var target in hitEntities)
            {
                if (!_xeno.CanAbilityAttackTarget(xeno, target, true, false))
                    continue;

                //apply damage
                if (_barricadeQuery.HasComp(target))
                {
                    var damage = _damage.TryChangeDamage(target, cadeDamage, origin: xeno, tool: xeno);
                }
                else
                {
                    var change = _damage.TryChangeDamage(target, xeno.Comp.DamageToMobs, origin: xeno, tool: xeno);
                    if (change?.GetTotal() > FixedPoint2.Zero)
                    {
                        var filter = Filter.Pvs(target, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
                        _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
                    }
                    hits++;
                }
            }
            if (hits > 0)
            {
                RefreshCooldowns(xeno, hits);
            }
        }

        //do telegraph
        foreach (var tile in explodingTiles)
        {
            if (!_interaction.InRangeUnobstructed(xeno.Owner, _turf.GetTileCenter(tile), xeno.Comp.Range + 0.5f))
                continue;

            SpawnAtPosition(xeno.Comp.TelegraphEffect, _turf.GetTileCenter(tile));
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
