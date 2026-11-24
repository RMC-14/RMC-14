using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class XenoRemoteStructureRemovalSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTacticalMapSystem _tacticalMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<TacticalMapUserComponent>(TacticalMapUserUi.Key,
            subs => subs.Event<XenoRemoteStructureRemovalBuiMsg>(OnRemovalRequest));

        SubscribeLocalEvent<TacticalMapUserComponent, XenoRemoteStructureRemovalConfirmEvent>(OnRemovalConfirmed);
    }

    private void OnRemovalRequest(Entity<TacticalMapUserComponent> queen, ref XenoRemoteStructureRemovalBuiMsg args)
    {
        if (_net.IsClient || !TryGetRemovableStructure(queen, args.Actor, args.Target, out var structure))
            return;

        var cancel = HasComp<TimedDespawnComponent>(structure);
        var title = Loc.GetString("rmc-xeno-remote-structure-removal-title");
        var structureName = FormattedMessage.EscapeText(Name(structure));
        var message = Loc.GetString(
            cancel
                ? "rmc-xeno-remote-structure-removal-cancel-confirm"
                : "rmc-xeno-remote-structure-removal-start-confirm",
            ("structure", structureName));

        var ev = new XenoRemoteStructureRemovalConfirmEvent(GetNetEntity(args.Actor), GetNetEntity(structure), cancel);
        _dialog.OpenConfirmation(queen, title, message, ev);
    }

    private void OnRemovalConfirmed(Entity<TacticalMapUserComponent> queen, ref XenoRemoteStructureRemovalConfirmEvent args)
    {
        if (_net.IsClient ||
            !TryGetEntity(args.Actor, out var actor) ||
            actor is not { } actorId ||
            !TryGetRemovableStructure(queen, actorId, args.Target, out var structure))
        {
            return;
        }

        var hasTimer = HasComp<TimedDespawnComponent>(structure);
        if (hasTimer != args.Cancel)
        {
            var stateChangedMsg = Loc.GetString("rmc-xeno-remote-structure-removal-state-changed");
            _popup.PopupClient(stateChangedMsg, queen, queen, PopupType.MediumCaution);
            return;
        }

        if (args.Cancel)
        {
            RemComp<TimedDespawnComponent>(structure);
            var cancelledMsg = Loc.GetString("rmc-xeno-remote-structure-removal-cancelled", ("structure", Name(structure)));
            _popup.PopupClient(cancelledMsg, queen, queen);
            _adminLog.Add(
                LogType.RMCXenoConstruct,
                $"Queen {ToPrettyString(queen):queen} cancelled remote removal of " +
                $"{ToPrettyString(structure):structure}");
            return;
        }

        var timer = EnsureComp<TimedDespawnComponent>(structure);
        timer.Lifetime = (float) structure.Comp.RemovalDelay.TotalSeconds;
        var startedMsg = Loc.GetString("rmc-xeno-remote-structure-removal-started", ("structure", Name(structure)));
        _popup.PopupClient(startedMsg, queen, queen);
        _adminLog.Add(
            LogType.RMCXenoConstruct,
            $"Queen {ToPrettyString(queen):queen} started remote removal of " +
            $"{ToPrettyString(structure):structure} in {timer.Lifetime} seconds");
    }

    private bool TryGetRemovableStructure(Entity<TacticalMapUserComponent> queen, EntityUid actor, NetEntity netTarget, out Entity<XenoRemoteStructureRemovalComponent> structure)
    {
        structure = default;

        if (actor != queen.Owner ||
            !HasComp<XenoOvipositorCapableComponent>(queen) ||
            !_mobState.IsAlive(queen))
        {
            return false;
        }

        if (!TryGetEntity(netTarget, out var target))
            return false;

        var targetId = target.Value;
        if (TerminatingOrDeleted(targetId) ||
            !TryComp(targetId, out XenoRemoteStructureRemovalComponent? removable))
        {
            return false;
        }

        if (removable.RemovalDelay < TimeSpan.Zero ||
            !_tacticalMap.IsXenoStructureOnUserMap(queen, targetId) ||
            !_hive.FromSameHive(queen.Owner, targetId))
        {
            return false;
        }

        structure = (targetId, removable);
        return true;
    }
}
