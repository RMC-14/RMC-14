using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

public sealed class ManageHiveSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedCommendationSystem _commendation = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly SharedXenoWatchSystem _xenoWatch = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private static readonly string[] JellyNames =
    [
        "Royal jelly of slaughter",
        "Royal jelly of resilience",
        "Royal jelly of sabotage",
        "Royal jelly of proliferation",
        "Royal jelly of rejuvenation",
    ];

    private int _jelliesPerQueen;

    public override void Initialize()
    {
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveActionEvent>(OnManageHiveAction);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveEvent>(OnManageHiveDevolve);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveJellyEvent>(OnManageHiveJelly);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveJellyXenoEvent>(OnManageHiveJellyXeno);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveJellyNameEvent>(OnManageHiveJellyType);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveJellyMessageEvent>(OnManageHiveJellyMessage);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveConfirmEvent>(OnManageHiveDevolveConfirm);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveMessageEvent>(OnManageHiveDevolveMessage);

        Subs.CVar(_config, RMCCVars.RMCJelliesPerQueen, v => _jelliesPerQueen = v, true);
    }

    private void OnManageHiveAction(Entity<ManageHiveComponent> manage, ref ManageHiveActionEvent args)
    {
        // TODO RMC14 other options
        var options = new List<DialogOption>
        {
            new("De-evolve (500)", new ManageHiveDevolveEvent())
        };

        if (TryComp(manage, out CommendationGiverComponent? giver) &&
            giver.Given < _jelliesPerQueen)
        {
            options.Add(new DialogOption("Reward Jelly (500)", new ManageHiveJellyEvent()));
        }

        _dialog.OpenOptions(manage, "Hive Management", options, "Manage The Hive");
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

    private void OnManageHiveJelly(Entity<ManageHiveComponent> ent, ref ManageHiveJellyEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(ent, out CommendationGiverComponent? giver) ||
            !TryComp(ent, out ActorComponent? giverActor))
        {
            return;
        }

        try
        {
            var playTimes = _playtime.GetPlayTimes(giverActor.PlayerSession);
            if (!playTimes.TryGetValue(ent.Comp.PlayTime, out var time) ||
                time < ent.Comp.JellyRequiredTime)
            {
                _popup.PopupCursor($"You don't have the time required to hand out jellies ({(int) ent.Comp.JellyRequiredTime.TotalHours} hours)", ent, PopupType.LargeCaution);
                return;
            }
        }
        catch
        {
            // ignored
        }

        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, ent.Comp.JellyPlasmaCost, false))
            return;

        var choices = new List<DialogOption>();
        var manageMemberComp = CompOrNull<HiveMemberComponent>(ent);
        var manageMember = new Entity<ManageHiveComponent?, CommendationGiverComponent?, HiveMemberComponent?, ActorComponent?>(ent, ent, giver, manageMemberComp, giverActor);
        var receivers = EntityQueryEnumerator<CommendationReceiverComponent, HiveMemberComponent>();
        while (receivers.MoveNext(out var uid, out _, out var member))
        {
            if (!CanAwardJellyPopup(manageMember, (uid, member), false))
                continue;

            choices.Add(new DialogOption(Name(uid), new ManageHiveJellyXenoEvent(GetNetEntity(uid))));
        }

        _dialog.OpenOptions(ent, "Jelly Recipient", choices, "Who do you want to award jelly to?");
    }

    private void OnManageHiveJellyXeno(Entity<ManageHiveComponent> ent, ref ManageHiveJellyXenoEvent args)
    {
        if (_net.IsClient)
            return;

        var options = new List<DialogOption>();
        foreach (var jellyName in JellyNames)
        {
            options.Add(new DialogOption(jellyName, new ManageHiveJellyNameEvent(args.Xeno, jellyName)));
        }

        _dialog.OpenOptions(ent, "Jelly Type", options, "What type of jelly do you want to award?");
    }

    private void OnManageHiveJellyType(Entity<ManageHiveComponent> ent, ref ManageHiveJellyNameEvent args)
    {
        if (_net.IsClient)
            return;

        var ev = new ManageHiveJellyMessageEvent(args.Xeno, args.Name);
        _dialog.OpenInput(ent, "What should the pheromone read?", ev, true, _commendation.CharacterLimit);
    }

    private void OnManageHiveJellyMessage(Entity<ManageHiveComponent> ent, ref ManageHiveJellyMessageEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        if (!CanAwardJellyPopup(ent.Owner, xeno.Value))
            return;

        if (!_commendation.ValidCommendation(ent.Owner, xeno.Value, args.Message))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.JellyPlasmaCost))
            return;

        _commendation.GiveCommendation(ent.Owner, xeno.Value, args.Name, args.Message, CommendationType.Jelly);
        _popup.PopupCursor("Jelly awarded", ent, PopupType.Large);
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
        if (!_xenoWatch.TryGetWatched(manage.Owner, out var watchedId) ||
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

    private bool CanAwardJellyPopup(Entity<ManageHiveComponent?, CommendationGiverComponent?, HiveMemberComponent?, ActorComponent?> manage, Entity<HiveMemberComponent?> target, bool popup = true)
    {
        if (!Resolve(manage, ref manage.Comp1, ref manage.Comp2, ref manage.Comp3, ref manage.Comp4, false))
            return false;

        if (!Resolve(target, ref target.Comp, false) ||
            !_hive.FromSameHive(manage.Owner, target) ||
            !TryComp(target, out CommendationReceiverComponent? receiver) ||
            receiver.LastPlayerId == null ||
            manage.Owner == target.Owner ||
            Guid.Parse(receiver.LastPlayerId) == manage.Comp4.PlayerSession.UserId)
        {
            if (popup)
                _popup.PopupCursor("You can't give a jelly to that xeno!", manage, PopupType.MediumCaution);

            return false;
        }

        if (manage.Comp2.Given >= _jelliesPerQueen)
        {
            if (popup)
                _popup.PopupCursor("You can't give out any more jellies!", manage, PopupType.MediumCaution);

            return false;
        }

        return true;
    }
}
