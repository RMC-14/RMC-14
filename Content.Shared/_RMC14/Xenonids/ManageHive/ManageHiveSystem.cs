using System.Linq;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

public sealed class ManageHiveSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly SharedWatchXenoSystem _watchXeno = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveActionEvent>(OnManageHiveAction);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveEvent>(OnManageHiveDevolve);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveConfirmEvent>(OnManageHiveDevolveConfirm);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveMessageEvent>(OnManageHiveDevolveMessage);
    }

    private void OnManageHiveAction(Entity<ManageHiveComponent> manage, ref ManageHiveActionEvent args)
    {
        // TODO RMC14 other options
        _dialog.OpenOptions(manage,
            "Hive Management",
            new List<DialogOption>
            {
                new("De-evolve (500)", new ManageHiveDevolveEvent()),
            },
            "Manage The Hive"
        );
    }

    private void OnManageHiveDevolve(Entity<ManageHiveComponent> manage, ref ManageHiveDevolveEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanDevolveTargetPopup(manage, out var watched))
            return;

        var devolutions = watched.Comp.DevolvesTo;
        if (devolutions.Length == 1)
        {
            var msg = $"Are you sure you want to deevolve {Name(watched)}";
            if (Prototype(watched)?.Name is { } name)
                msg += $" from {name}";

            var devolves = devolutions[0];
            if (_prototype.TryIndex(devolves, out var devolveProto))
                msg += $" to {devolveProto.Name}?";

            _dialog.OpenConfirmation(manage, "Deevolution", msg, new ManageHiveDevolveConfirmEvent(devolves));
            return;
        }

        var choices = new List<DialogOption>();
        foreach (var choice in devolutions)
        {
            var name = choice.Id;
            if (_prototype.TryIndex(choice, out var choiceProto))
                name = choiceProto.Name;

            choices.Add(new DialogOption(name, new ManageHiveDevolveConfirmEvent(choice)));
        }

        _dialog.OpenOptions(manage, "Choose a caste", choices);
    }

    private void OnManageHiveDevolveConfirm(Entity<ManageHiveComponent> manage, ref ManageHiveDevolveConfirmEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanDevolveTargetPopup(manage, out var watched) ||
            !watched.Comp.DevolvesTo.Contains(args.Choice.Id))
        {
            return;
        }

        _dialog.OpenInput(manage, $"Provide a reason for deevolving {Name(watched)}", new ManageHiveDevolveMessageEvent(args.Choice));
    }

    private void OnManageHiveDevolveMessage(Entity<ManageHiveComponent> manage, ref ManageHiveDevolveMessageEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanDevolveTargetPopup(manage, out var watched) ||
            !watched.Comp.DevolvesTo.Contains(args.Choice))
        {
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(manage.Owner, manage.Comp.DevolvePlasmaCost))
            return;

        var oldString = ToPrettyString(watched);
        if (_xenoEvolution.Devolve(watched, args.Choice) is not { } devolution)
            return;

        if (TryComp(devolution, out ActorComponent? watchedActor))
        {
            var msg = $"The queen is deevolving you for the following reason: {args.Message}";
            _rmcChat.ChatMessageToOne(ChatChannel.Local, msg, msg, default, false, watchedActor.PlayerSession.Channel);
            _popup.PopupEntity(msg, devolution, PopupType.LargeCaution);
        }

        // TODO RMC14 drop dead acidic heart
        _adminLog.Add(LogType.RMCDevolve, $"{ToPrettyString(manage)} devolved {oldString} to {ToPrettyString(devolution)}");
    }

    private bool CanDevolveTargetPopup(Entity<ManageHiveComponent> manage, out Entity<XenoDevolveComponent> watched)
    {
        watched = default;
        if (!_watchXeno.TryGetWatched(manage.Owner, out var watchedId) ||
            watchedId == manage.Owner)
        {
            _popup.PopupEntity("You must overwatch the xeno you want to de-evolve.", manage, manage, PopupType.MediumCaution);
            return false;
        }

        if (!TryComp(watchedId, out XenoDevolveComponent? devolve) ||
            devolve.DevolvesTo.Length == 0)
        {
            _popup.PopupEntity($"{Name(watchedId)} can't be devolved!", watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        if (!devolve.CanBeDevolvedByOther)
        {
            _popup.PopupEntity("You cannot deevolve xenonids to larva.", watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        if (!_xenoPlasma.HasPlasmaPopup(manage.Owner, manage.Comp.DevolvePlasmaCost, false))
            return false;

        if (!_hive.FromSameHive(manage.Owner, watchedId))
        {
            _popup.PopupEntity("You cannot deevolve a member of another hive!", watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        watched = (watchedId, devolve);
        return true;
    }
}
