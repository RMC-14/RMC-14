using System.Numerics;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.AcidMine;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Insight;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Xenonids.DeployTraps;

public sealed class XenoDeployTrapsSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly XenoInsightSystem _insight = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeployTrapsComponent, XenoDeployTrapsActionEvent>(OnXenoDeployTrapsAction);
    }

    private void OnXenoDeployTrapsAction(Entity<XenoDeployTrapsComponent> xeno, ref XenoDeployTrapsActionEvent args)
    {
        if (args.Handled)
            return;

        // Check if target on grid
        if (_transform.GetGrid(args.Target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        if (!_interaction.InRangeUnobstructed(
                _transform.GetMapCoordinates(xeno.Owner),
                _transform.ToMapCoordinates(args.Target),
                xeno.Comp.Range,
                CollisionGroup.Opaque,
                e => e == xeno.Owner || !Transform(e).Anchored))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-deploy-traps-see-fail"), xeno, xeno);
            return;
        }

        // Check if user has enough plasma
        if (!_xenoPlasma.TryRemovePlasmaPopup((xeno.Owner, null), args.PlasmaCost))
            return;

        args.Handled = true;

        var coords = args.Target.SnapToGrid(EntityManager, _map);

        _audio.PlayPredicted(xeno.Comp.DeploySound, coords, xeno);

        var popupSelf = Loc.GetString("rmc-xeno-deploy-traps-self");
        var popupOthers = Loc.GetString("rmc-xeno-deploy-traps-others", ("xeno", xeno));
        _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

        if (_net.IsServer)
        {
            var xenoPos = _transform.ToWorldPosition(xeno.Owner.ToCoordinates());
            var targetPos = _transform.ToWorldPosition(coords);

            var direction = (targetPos - xenoPos).Normalized();
            var ortho = new Vector2(-direction.Y, direction.X);

            // Project to range, then extend orthogonally
            //+1 to get the 5 traps we want, rather than 4.
            var tip = targetPos;
            var trapStart = new EntityCoordinates(gridId, tip + ortho * (xeno.Comp.DeployTrapsRadius + 1));
            var trapEnd = new EntityCoordinates(gridId, tip - ortho * xeno.Comp.DeployTrapsRadius);

            var trapTiles = _line.DrawLine(trapStart, trapEnd, TimeSpan.Zero, xeno.Comp.Range, out _);

            var empowered = xeno.Comp.Empowered;

            foreach (var tile in trapTiles)
            {
                var turfCoords = new EntityCoordinates(gridId, tile.Coordinates.Position);
                var blocked = _rmcMap.HasAnchoredEntityEnumerator<DeployTrapsBlockerComponent>(turfCoords, out _);
                if (!blocked)
                {
                    DeployTraps(xeno, turfCoords, empowered);
                }
            }

            if (empowered)
            {
                DeployTrapsEmpower(xeno);
                if (xeno.Comp.Emote is { } emote)
                    _emote.TryEmoteWithChat(xeno, emote, false, null, false, true);
                xeno.Comp.Empowered = false;
                _insight.IncrementInsight(xeno.Owner, -10);
                foreach (var action in _actions.GetActions(xeno.Owner))
                {
                    if (_actions.GetEvent(action) is XenoDeployTrapsActionEvent)
                        _actions.SetIcon(action.AsNullable(), xeno.Comp.ActionIcon);
                }
            }
        }
    }

    private void DeployTraps(Entity<XenoDeployTrapsComponent> xeno, EntityCoordinates target, bool empowered)
    {
        if (!target.IsValid(EntityManager))
            return;

        if (_net.IsServer)
        {
            if (empowered)
            {
                var traps = SpawnAtPosition(xeno.Comp.DeployEmpoweredTrapsId, target);
                _hive.SetSameHive(xeno.Owner, traps);
            }
            else
            {
                var traps = SpawnAtPosition(xeno.Comp.DeployTrapsId, target);
                _hive.SetSameHive(xeno.Owner, traps);
            }
        }
    }

    private void DeployTrapsEmpower(Entity<XenoDeployTrapsComponent> xeno)
    {
        if (!_net.IsServer)
            return;

        if (TryComp(xeno.Owner, out XenoAcidMineComponent? acidMine))
            acidMine.Empowered = true;
        _popup.PopupPredicted(Loc.GetString("rmc-xeno-deploy-traps-empower"), xeno, xeno, PopupType.Medium);
        foreach (var action in _actions.GetActions(xeno.Owner))
        {
            if (_actions.GetEvent(action) is XenoAcidMineActionEvent)
                _actions.SetIcon(action.AsNullable(), acidMine?.ActionIconEmpowered);
        }

    }
}
