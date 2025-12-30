using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Mobs;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.DragDrop;
using Content.Shared.Mind;

namespace Content.Shared._RMC14.Mobs;

public sealed class RMCAdminGhostDragPossessionSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMGhostComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<CMGhostComponent, CanDropDraggedEvent>(OnCanDropDragged);
        SubscribeLocalEvent<CMGhostComponent, DragDropDraggedEvent>(OnGhostDraggedDropped);
        SubscribeLocalEvent<CMGhostComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<CMGhostComponent, GhostPossessionConfirmEvent>(OnPossessionConfirmation);
    }

    private void OnCanDrag(Entity<CMGhostComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCanDropDragged(Entity<CMGhostComponent> ent, ref CanDropDraggedEvent args)
    {
        if (!Exists(ent) || !_adminManager.HasAdminFlag(args.User, AdminFlags.Fun))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnCanDropTarget(Entity<CMGhostComponent> ent, ref CanDropTargetEvent args)
    {
        if (!Exists(ent) || !_adminManager.HasAdminFlag(args.User, AdminFlags.Fun))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnGhostDraggedDropped(Entity<CMGhostComponent> ent, ref DragDropDraggedEvent args)
    {
        if (!Exists(ent) || !_adminManager.HasAdminFlag(args.User, AdminFlags.Fun) || ent.Owner == args.Target)
            return;
        args.Handled = true;

        var ev = new GhostPossessionConfirmEvent(GetNetEntity(args.User), GetNetEntity(ent), GetNetEntity(args.Target));

        _dialog.OpenConfirmation(
            args.User,
            "Are you sure?",
            $"Are you sure you want [Bold][Italic]{MetaData(ent).EntityName} | {ent.Owner.Id}[/Bold][/Italic] to possess [Bold][Italic]{MetaData(args.Target).EntityName} | {args.Target.Id}[/Bold][/Italic]",
            ev);
    }
    private void OnPossessionConfirmation(Entity<CMGhostComponent> ent, ref GhostPossessionConfirmEvent args)
    {
        var actor = GetEntity(args.Actor);
        var possessor = GetEntity(args.Possessor);
        var toPossess = GetEntity(args.ToPossess);

        _mind.ControlMob(possessor, toPossess);

        _adminLog.Add(LogType.RMCAdminCommandLogging,
            LogImpact.High,
            $"{ToPrettyString(actor):player} has forced {ToPrettyString(possessor):entity} to possess {ToPrettyString(toPossess):player}");
    }
}
