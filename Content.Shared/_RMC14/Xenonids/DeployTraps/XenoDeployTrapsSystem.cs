using System.Numerics;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.AcidMine;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Insight;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
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
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly XenoInsightSystem _insight = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDeployTrapsComponent, XenoDeployTrapsActionEvent>(OnXenoDeployTrapsAction);
        SubscribeLocalEvent<XenoDeployTrapsComponent, XenoDeployTrapsDoAfter>(OnDeployTrapsDoAfter);
    }

    private void OnXenoDeployTrapsAction(Entity<XenoDeployTrapsComponent> xeno, ref XenoDeployTrapsActionEvent args)
    {
        if (args.Handled)
            return;

        // Check if target on grid
        if (_transform.GetGrid(args.Target) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        if (!_examine.InRangeUnOccluded(xeno.Owner, args.Target, xeno.Comp.Range))
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-deploy-traps-see-fail"), xeno, xeno);
            return;
        }

        args.Handled = true;

        var target = args.Target.SnapToGrid(EntityManager, _map);

        // Check if user has enough plasma
        if (xeno.Comp.DeployTrapsDoAfter != null ||
            !_xenoPlasma.TryRemovePlasmaPopup((xeno.Owner, null), args.PlasmaCost))
            return;

        // Deploy Traps
        var ev = new XenoDeployTrapsDoAfter(GetNetCoordinates(target));
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.DeployTrapsDoAfterPeriod, ev, xeno)
            { BreakOnMove = false, DuplicateCondition = DuplicateConditions.SameEvent };
        if (_doAfter.TryStartDoAfter(doAfter, out var id))
        {
            xeno.Comp.DeployTrapsDoAfter = id;
        }
    }

    private void OnDeployTrapsDoAfter(Entity<XenoDeployTrapsComponent> xeno, ref XenoDeployTrapsDoAfter args)
    {
        xeno.Comp.DeployTrapsDoAfter = null;

        if (args.Cancelled)
            return;

        var coords = GetCoordinates(args.Coordinates);
        if (_transform.GetGrid(coords) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
            return;

        var popupSelf = Loc.GetString("rmc-xeno-deploy-traps-self");
        var popupOthers = Loc.GetString("rmc-xeno-deploy-traps-others", ("xeno", xeno));
        _popup.PopupPredicted(popupSelf, popupOthers, xeno, xeno);

        if (_net.IsServer)
        {
            // All math in world space using Vector2
            var xenoPos = _transform.ToWorldPosition(xeno.Owner.ToCoordinates());
            var targetPos = _transform.ToWorldPosition(coords);

            var direction = (targetPos - xenoPos).Normalized();
            var ortho = new Vector2(-direction.Y, direction.X);

            // Project to range, then extend orthogonally
            var tip = targetPos;
            var trapStart = new EntityCoordinates(gridId, tip + ortho * xeno.Comp.DeployTrapsRadius);
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
                    _audio.PlayPredicted(xeno.Comp.DeploySound, turfCoords, xeno);
                }
            }

            if (empowered)
            {
                DeployTrapsEmpower(xeno);
                if (xeno.Comp.Emote is { } emote)
                    _emote.TryEmoteWithChat(xeno, emote);
                xeno.Comp.Empowered = false;
                _insight.IncrementInsight(xeno.Owner, -10);
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

    public void DeployTrapsEmpower(Entity<XenoDeployTrapsComponent> xeno)
    {
        if (!_net.IsServer)
            return;

        if (TryComp(xeno.Owner, out XenoAcidMineComponent? acidMine))
            acidMine.Empowered = true;
        _popup.PopupClient(Loc.GetString("rmc-xeno-deploy-traps-empower"), xeno, xeno, PopupType.Medium);
    }
}
