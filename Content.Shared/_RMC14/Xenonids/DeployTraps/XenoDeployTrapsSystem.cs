using System.Numerics;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.AcidMine;
using Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Insight;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Xenonids.DeployTraps;

public sealed class XenoDeployTrapsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoInsightSystem _insight = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly XenoAcidMineSystem _acidMine = default!;

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

        var targetMap = _transform.ToMapCoordinates(args.Target);
        var tileBase = new Vector2(targetMap.Position.Floored().X, targetMap.Position.Floored().Y);
        var origin = _transform.GetMapCoordinates(xeno.Owner);
        var tileCenter = new MapCoordinates(tileBase + new Vector2(0.5f, 0.5f), targetMap.MapId);
        if ((tileCenter.Position - origin.Position).LengthSquared() > xeno.Comp.Range * xeno.Comp.Range)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-deploy-traps-range-fail"), xeno, xeno);
            return;
        }

        var offsets = new Vector2[]
        {
            new(0.5f, 0.5f),   // centre
            new(0.2f, 0.2f),   // bottom-left
            new(0.8f, 0.2f),   // bottom-right
            new(0.2f, 0.8f),   // top-left
            new(0.8f, 0.8f),   // top-right
        };

        var hasLos = false;
        foreach (var offset in offsets)
        {
            var point = new MapCoordinates(tileBase + offset, targetMap.MapId);
            if (_examine.InRangeUnOccluded(xeno, point, xeno.Comp.Range))
            {
                hasLos = true;
                break;
            }
        }
        if (!hasLos)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-deploy-traps-see-fail"), xeno, xeno);
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup((xeno.Owner, null), xeno.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var coords = args.Target.SnapToGrid(EntityManager, _map);

        _audio.PlayPredicted(xeno.Comp.DeploySound, coords, xeno);

        var popupSelf = Loc.GetString("rmc-xeno-deploy-traps-self");
        var popupOthers = Loc.GetString("rmc-xeno-deploy-traps-others");
        _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

        if (_net.IsServer)
        {
            var xenoPos = _transform.ToWorldPosition(xeno.Owner.ToCoordinates());
            var targetPos = _transform.ToWorldPosition(coords);

            var direction = (targetPos - xenoPos).Normalized();
            var ortho = new Vector2(-direction.Y, direction.X);

            // Round orthogonal world vector to nearest tile-space step
            var orthoTile = new Vector2i(
                (int) MathF.Round(ortho.X),
                (int) MathF.Round(ortho.Y)
            );

            if (orthoTile == Vector2i.Zero)
                orthoTile = new Vector2i(1, 0);

            var centerTile = _mapSystem.CoordinatesToTile(gridId, grid, targetMap);

            var radius = (int) xeno.Comp.DeployTrapsRadius;
            var empowered = xeno.Comp.Empowered;

            for (var i = -radius; i <= radius; i++)
            {
                var tileIndex = centerTile + orthoTile * i;
                var tileCoords = new EntityCoordinates(gridId, _mapSystem.GridTileToLocal(gridId, grid, tileIndex).Position);
                if(!_rmcMap.HasAnchoredEntityEnumerator<DeployTrapsBlockerComponent>(tileCoords, out _))
                    DeployTraps(xeno, tileCoords, empowered);
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
            var deployId = empowered ? xeno.Comp.DeployEmpoweredTrapsId : xeno.Comp.DeployTrapsId;
            var traps = SpawnAtPosition(deployId, target);
            _hive.SetSameHive(xeno.Owner, traps);
            EnsureComp<XenoNewlyDeployedTrapsComponent>(traps);
            var comp = EnsureComp<XenoDeployedTrapsComponent>(traps);
            comp.PlacedBy = xeno.Owner;
            Dirty(traps, comp);
        }
    }

    private void DeployTrapsEmpower(Entity<XenoDeployTrapsComponent> xeno)
    {
        if (TryComp(xeno.Owner, out XenoAcidMineComponent? acidMine))
            _acidMine.EmpowerAcidMine((xeno.Owner, acidMine));

        _popup.PopupPredicted(Loc.GetString("rmc-xeno-deploy-traps-empower"), xeno, xeno, PopupType.Medium);
    }
}
