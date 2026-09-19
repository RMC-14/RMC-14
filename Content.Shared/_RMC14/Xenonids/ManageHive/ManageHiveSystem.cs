using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.ManageHive.Boons;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared._RMC14.Xenonids.ManageHive;

public sealed class ManageHiveSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedCommendationSystem _commendation = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly HiveBoonSystem _hiveBoon = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _xenoAnnounce = default!;
    [Dependency] private readonly SharedXenoWatchSystem _xenoWatch = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private LocalizedDatasetPrototype _jelliesDataset = default!;

    private int _jelliesPerQueen;
    private TimeSpan _burrowedLarvaSacrificeTime;
    private int _burrowedLarvaEvolutionPointsPer;

    public override void Initialize()
    {
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveActionEvent>(OnManageHiveAction);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveEvent>(OnManageHiveDevolve);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveJellyEvent>(OnManageHiveJelly);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveSacrificeBurrowedEvent>(OnSacrificeBurrowed);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveSacrificeBurrowedTargetEvent>(OnSacrificeBurrowedTarget);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveActivateBoonsEvent>(OnPurchaseBoons);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveActivateBoonsChosenEvent>(OnPurchaseBoonsChosen);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveJellyXenoEvent>(OnManageHiveJellyXeno);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveJellyNameEvent>(OnManageHiveJellyType);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveJellyMessageEvent>(OnManageHiveJellyMessage);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveConfirmEvent>(OnManageHiveDevolveConfirm);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveDevolveMessageEvent>(OnManageHiveDevolveMessage);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveTeamsEvent>(OnManageHiveTeams);

        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsEvent>(OnManageHivePermissions);
        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsHarmEvent>(OnManageHivePermissionsHarm);
        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsHarmChosenEvent>(OnManageHivePermissionsHarmChosen);
        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsConstructionEvent>(OnManageHivePermissionsConstruction);
        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsConstructionChosenEvent>(OnManageHivePermissionsConstructionChosen);
        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsDeconstructionEvent>(OnManageHivePermissionsDeconstruction);
        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsDeconstructionChosenEvent>(OnManageHivePermissionsDeconstructionChosen);
        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsUnnestEvent>(OnManageHivePermissionsUnnest);
        SubscribeLocalEvent<ManageHiveComponent, ManageHivePermissionsUnnestChosenEvent>(OnManageHivePermissionsUnnestChosen);

        Subs.CVar(_config, RMCCVars.RMCJelliesPerQueen, v => _jelliesPerQueen = v, true);
        Subs.CVar(_config, RMCCVars.RMCBurrowedLarvaSacrificeTimeMinutes, v => _burrowedLarvaSacrificeTime = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCBurrowedLarvaEvolutionPointsPer, v => _burrowedLarvaEvolutionPointsPer = v, true);

        _jelliesDataset = _prototype.Index<LocalizedDatasetPrototype>(SharedCommendationSystem.JellyDatasetId);
    }

    private void OnManageHiveAction(Entity<ManageHiveComponent> manage, ref ManageHiveActionEvent args)
    {
        // TODO RMC14 other options
        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-hivemanagement-deevolve"), new ManageHiveDevolveEvent())
        };

        if (TryComp(manage, out CommendationGiverComponent? giver) &&
            giver.Given < _jelliesPerQueen)
        {
            options.Add(new DialogOption(Loc.GetString("rmc-hivemanagement-reward"), new ManageHiveJellyEvent()));
        }

        options.Add(new DialogOption(Loc.GetString("rmc-hivemanagement-exchange-larva"), new ManageHiveSacrificeBurrowedEvent()));
        options.Add(new DialogOption(Loc.GetString("rmc-boon-activate"), new ManageHiveActivateBoonsEvent()));
        options.Add(new DialogOption(Loc.GetString("rmc-hivemanagement-manage-teams"), new ManageHiveTeamsEvent()));
        options.Add(new DialogOption(Loc.GetString("rmc-hivemanagement-permissions"), new ManageHivePermissionsEvent()));

        _dialog.OpenOptions(manage, Loc.GetString("rmc-hivemanagement-hive-management"), options, Loc.GetString("rmc-hivemanagement-manage-the-hive"));
    }

    private void OnManageHivePermissions(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsEvent args)
    {
        if (_net.IsClient)
            return;

        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-hivemanagement-permissions-harming"), new ManageHivePermissionsHarmEvent()),
            new(Loc.GetString("rmc-hivemanagement-permissions-construction"), new ManageHivePermissionsConstructionEvent()),
            new(Loc.GetString("rmc-hivemanagement-permissions-deconstruction"), new ManageHivePermissionsDeconstructionEvent()),
            new(Loc.GetString("rmc-hivemanagement-permissions-unnesting"), new ManageHivePermissionsUnnestEvent()),
        };

        _dialog.OpenOptions(manage, Loc.GetString("rmc-hivemanagement-permissions-title"), options);
    }

    private void OnManageHivePermissionsHarm(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsHarmEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanChangePermissionPopup(manage, out var hive, h => h.Comp.HarmPermissionChangeAt))
            return;

        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-hivemanagement-permissions-harm-forbidden"), new ManageHivePermissionsHarmChosenEvent(XenoHarmPermission.Forbidden)),
            new(Loc.GetString("rmc-hivemanagement-permissions-harm-restricted"), new ManageHivePermissionsHarmChosenEvent(XenoHarmPermission.RestrictedInfected)),
            new(Loc.GetString("rmc-hivemanagement-permissions-harm-allowed"), new ManageHivePermissionsHarmChosenEvent(XenoHarmPermission.Allowed)),
        };

        var current = Loc.GetString(GetPermissionLoc(hive.Comp.HarmPermission));
        _dialog.OpenOptions(manage, Loc.GetString("rmc-hivemanagement-permissions-harming"), options, Loc.GetString("rmc-hivemanagement-permissions-current", ("value", current)));
    }

    private void OnManageHivePermissionsHarmChosen(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsHarmChosenEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanChangePermissionPopup(manage, out var hive, h => h.Comp.HarmPermissionChangeAt))
            return;

        if (hive.Comp.HarmPermission == args.Choice)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-permissions-already-set"), manage, manage, PopupType.MediumCaution);
            return;
        }

        _hive.SetHarmPermission(hive, args.Choice);

        var msg = Loc.GetString("rmc-hivemanagement-permissions-harm-announce", ("value", Loc.GetString(GetPermissionLoc(args.Choice))));
        _xenoAnnounce.AnnounceToHive(manage.Owner, hive, msg);
    }

    private void OnManageHivePermissionsConstruction(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsConstructionEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanChangePermissionPopup(manage, out var hive, h => h.Comp.ConstructionPermissionChangeAt))
            return;

        var options = GetConstructionPermissionOptions(false);
        var current = Loc.GetString(GetPermissionLoc(hive.Comp.ConstructionPermission));
        _dialog.OpenOptions(manage, Loc.GetString("rmc-hivemanagement-permissions-construction"), options, Loc.GetString("rmc-hivemanagement-permissions-current", ("value", current)));
    }

    private void OnManageHivePermissionsConstructionChosen(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsConstructionChosenEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanChangePermissionPopup(manage, out var hive, h => h.Comp.ConstructionPermissionChangeAt))
            return;

        if (hive.Comp.ConstructionPermission == args.Choice)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-permissions-already-set"), manage, manage, PopupType.MediumCaution);
            return;
        }

        _hive.SetConstructionPermission(hive, args.Choice);

        var msg = Loc.GetString("rmc-hivemanagement-permissions-construction-announce", ("value", Loc.GetString(GetPermissionLoc(args.Choice))));
        _xenoAnnounce.AnnounceToHive(manage.Owner, hive, msg);
    }

    private void OnManageHivePermissionsDeconstruction(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsDeconstructionEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanChangePermissionPopup(manage, out var hive, h => h.Comp.DeconstructionPermissionChangeAt))
            return;

        var options = GetConstructionPermissionOptions(true);
        var current = Loc.GetString(GetPermissionLoc(hive.Comp.DeconstructionPermission));
        _dialog.OpenOptions(manage, Loc.GetString("rmc-hivemanagement-permissions-deconstruction"), options, Loc.GetString("rmc-hivemanagement-permissions-current", ("value", current)));
    }

    private void OnManageHivePermissionsDeconstructionChosen(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsDeconstructionChosenEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanChangePermissionPopup(manage, out var hive, h => h.Comp.DeconstructionPermissionChangeAt))
            return;

        if (hive.Comp.DeconstructionPermission == args.Choice)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-permissions-already-set"), manage, manage, PopupType.MediumCaution);
            return;
        }

        _hive.SetDeconstructionPermission(hive, args.Choice);

        var msg = Loc.GetString("rmc-hivemanagement-permissions-deconstruction-announce", ("value", Loc.GetString(GetPermissionLoc(args.Choice))));
        _xenoAnnounce.AnnounceToHive(manage.Owner, hive, msg);
    }

    private void OnManageHivePermissionsUnnest(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsUnnestEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanChangePermissionPopup(manage, out var hive, h => h.Comp.UnnestPermissionChangeAt))
            return;

        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-hivemanagement-permissions-unnest-builders"), new ManageHivePermissionsUnnestChosenEvent(XenoUnnestPermission.Builders)),
            new(Loc.GetString("rmc-hivemanagement-permissions-level-anyone"), new ManageHivePermissionsUnnestChosenEvent(XenoUnnestPermission.Anyone)),
        };

        var current = Loc.GetString(GetPermissionLoc(hive.Comp.UnnestPermission));
        _dialog.OpenOptions(manage, Loc.GetString("rmc-hivemanagement-permissions-unnesting"), options, Loc.GetString("rmc-hivemanagement-permissions-current", ("value", current)));
    }

    private void OnManageHivePermissionsUnnestChosen(Entity<ManageHiveComponent> manage, ref ManageHivePermissionsUnnestChosenEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanChangePermissionPopup(manage, out var hive, h => h.Comp.UnnestPermissionChangeAt))
            return;

        if (hive.Comp.UnnestPermission == args.Choice)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-permissions-already-set"), manage, manage, PopupType.MediumCaution);
            return;
        }

        _hive.SetUnnestPermission(hive, args.Choice);

        var msg = Loc.GetString("rmc-hivemanagement-permissions-unnest-announce", ("value", Loc.GetString(GetPermissionLoc(args.Choice))));
        _xenoAnnounce.AnnounceToHive(manage.Owner, hive, msg);
    }

    private List<DialogOption> GetConstructionPermissionOptions(bool deconstruction)
    {
        DialogOption Make(XenoConstructionPermission permission)
        {
            var ev = deconstruction
                ? (object) new ManageHivePermissionsDeconstructionChosenEvent(permission)
                : new ManageHivePermissionsConstructionChosenEvent(permission);
            return new DialogOption(Loc.GetString(GetPermissionLoc(permission)), ev);
        }

        return new List<DialogOption>
        {
            Make(XenoConstructionPermission.Queen),
            Make(XenoConstructionPermission.Leaders),
            Make(XenoConstructionPermission.Anyone),
        };
    }

    private bool CanChangePermissionPopup(Entity<ManageHiveComponent> manage, out Entity<HiveComponent> hive, Func<Entity<HiveComponent>, TimeSpan?> getChangeAt)
    {
        hive = default;
        if (_hive.GetHive(manage.Owner) is not { } userHive)
            return false;

        hive = userHive;
        if (_hive.IsPermissionChangeOnCooldown(getChangeAt(hive), out var remaining))
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-permissions-cooldown", ("seconds", (int) remaining.TotalSeconds)), manage, manage, PopupType.MediumCaution);
            return false;
        }

        return true;
    }

    private static string GetPermissionLoc(XenoHarmPermission permission)
    {
        return permission switch
        {
            XenoHarmPermission.Forbidden => "rmc-hivemanagement-permissions-harm-forbidden",
            XenoHarmPermission.RestrictedInfected => "rmc-hivemanagement-permissions-harm-restricted",
            XenoHarmPermission.Allowed => "rmc-hivemanagement-permissions-harm-allowed",
            _ => string.Empty,
        };
    }

    private static string GetPermissionLoc(XenoConstructionPermission permission)
    {
        return permission switch
        {
            XenoConstructionPermission.Queen => "rmc-hivemanagement-permissions-level-queen",
            XenoConstructionPermission.Leaders => "rmc-hivemanagement-permissions-level-leaders",
            XenoConstructionPermission.Anyone => "rmc-hivemanagement-permissions-level-anyone",
            _ => string.Empty,
        };
    }

    private static string GetPermissionLoc(XenoUnnestPermission permission)
    {
        return permission switch
        {
            XenoUnnestPermission.Builders => "rmc-hivemanagement-permissions-unnest-builders",
            XenoUnnestPermission.Anyone => "rmc-hivemanagement-permissions-level-anyone",
            _ => string.Empty,
        };
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
            var name = Name(watched);
            string? protoName = null;
            if (Prototype(watched)?.Name is { } n)
                protoName = n;
            var hasFrom = protoName != null;
            var hasTo = _prototype.TryIndex(devolutions[0], out var devolveProto);
            string msg;
            if (hasFrom && hasTo)
                msg = Loc.GetString("rmc-hivemanagement-are-you-sure-deevolve-from-to", ("name", name), ("from", protoName ?? ""), ("to", devolveProto?.Name ?? ""));
            else if (hasFrom)
                msg = Loc.GetString("rmc-hivemanagement-are-you-sure-deevolve-from", ("name", name), ("from", protoName ?? ""));
            else
                msg = Loc.GetString("rmc-hivemanagement-are-you-sure-deevolve", ("name", name));

            _dialog.OpenConfirmation(manage, Loc.GetString("rmc-hivemanagement-deevolution"), msg, new ManageHiveDevolveConfirmEvent(devolutions[0]));
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

        _dialog.OpenOptions(manage, Loc.GetString("rmc-hivemanagement-choose-caste"), choices);
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
                _popup.PopupCursor(Loc.GetString("rmc-jelly-error-not-enough-playtime", ("requiredHours", (int) ent.Comp.JellyRequiredTime.TotalHours)), ent, PopupType.LargeCaution);
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

            choices.Add(new DialogOption(Name(uid), new ManageHiveJellyXenoEvent(GetNetEntity(uid), null)));
        }

        if (manageMemberComp != null && manageMemberComp.Hive != null && TryComp<HiveComponent>(manageMemberComp.Hive, out var hive))
        {
            foreach (var gibbed in hive.GibbedXenos)
            {
                if (!CanAwardJellyPopup(manageMember, gibbed, false))
                    continue;

                choices.Add(new DialogOption(gibbed.Name, new ManageHiveJellyXenoEvent(null, gibbed)));
            }
        }

        _dialog.OpenOptions(ent, Loc.GetString("rmc-jelly-recipient"), choices, Loc.GetString("rmc-jelly-recipient-prompt"));
    }

    private void OnSacrificeBurrowed(Entity<ManageHiveComponent> ent, ref ManageHiveSacrificeBurrowedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanSacrificeBurrowedPopup(ent, out _))
            return;

        var choices = new List<DialogOption>();
        var query = EntityQueryEnumerator<ActorComponent, XenoComponent, XenoEvolutionComponent>();
        while (query.MoveNext(out var target, out _, out _, out var evolution))
        {
            if (target == ent.Owner)
                continue;

            if (_mobState.IsIncapacitated(target))
                continue;

            var points = evolution.Points;
            var max = evolution.Max;
            if (evolution.Points >= evolution.Max)
                continue;

            if (!_hive.FromSameHive(ent.Owner, target))
                continue;

            var targetName = $"{Name(target)} ({points.Int()}/{max.Int()})";
            var ev = new ManageHiveSacrificeBurrowedTargetEvent(GetNetEntity(target));
            choices.Add(new DialogOption(targetName, ev));
        }

        _dialog.OpenOptions(ent, Loc.GetString("rmc-hivemanagement-exchange-larva-title"), choices, Loc.GetString("rmc-hivemanagement-exchange-larva-description", ("points", _burrowedLarvaEvolutionPointsPer)));
    }

    private void OnSacrificeBurrowedTarget(Entity<ManageHiveComponent> ent, ref ManageHiveSacrificeBurrowedTargetEvent args)
    {
        if (_net.IsClient)
            return;

        if (GetEntity(args.Target) is not { Valid: true } target ||
            ent.Owner == target ||
            !_hive.FromSameHive(ent.Owner, target) ||
            _mobState.IsIncapacitated(target))
        {
            return;
        }

        if (!CanSacrificeBurrowedPopup(ent, out var hive))
            return;

        _hive.ChangeBurrowedLarva(hive, -1);
        var given = _xenoEvolution.AddPointsCapped(target, _burrowedLarvaEvolutionPointsPer);

        _popup.PopupCursor(Loc.GetString("rmc-hivemanagement-exchange-larva-given-user", ("target", ent), ("points", given)), ent);
        _popup.PopupCursor(Loc.GetString("rmc-hivemanagement-exchange-larva-given-target", ("user", ent), ("points", given)), ent);
    }

    private void OnPurchaseBoons(Entity<ManageHiveComponent> ent, ref ManageHiveActivateBoonsEvent args)
    {
        if (_net.IsClient)
            return;

        var choices = new List<DialogOption>();
        foreach (var boon in _hiveBoon.Boons)
        {
            var text = Loc.GetString("rmc-boon-name-cost",
                ("boon", boon.Prototype.Name),
                ("cost", boon.Component.Cost),
                ("pylons", boon.Component.Pylons)
            );

            var ev = new ManageHiveActivateBoonsChosenEvent(boon.Prototype.ID);
            choices.Add(new DialogOption(text, ev));
        }

        var resin = 0;
        if (_hive.GetHive(ent.Owner) is { } hive)
            resin = _hiveBoon.EnsureBoons(hive).Comp.RoyalResin;

        _dialog.OpenOptions(ent, Loc.GetString("rmc-boon-activate"), choices, Loc.GetString("rmc-boon-message", ("current", resin)));
    }

    private void OnPurchaseBoonsChosen(Entity<ManageHiveComponent> ent, ref ManageHiveActivateBoonsChosenEvent args)
    {
        if (_net.IsClient)
            return;

        _hiveBoon.TryActivateBoon(ent, args.Boon);
    }

    private void OnManageHiveJellyXeno(Entity<ManageHiveComponent> ent, ref ManageHiveJellyXenoEvent args)
    {
        if (_net.IsClient)
            return;

        var options = new List<DialogOption>();
        foreach (var name in _jelliesDataset.Values)
        {
            options.Add(new DialogOption(Loc.GetString(name), new ManageHiveJellyNameEvent(args.Xeno, args.Gibbed, Loc.GetString(name))));
        }

        _dialog.OpenOptions(ent, Loc.GetString("rmc-jelly-type"), options, Loc.GetString("rmc-jelly-type-prompt"));
    }

    private void OnManageHiveJellyType(Entity<ManageHiveComponent> ent, ref ManageHiveJellyNameEvent args)
    {
        if (_net.IsClient)
            return;

        var ev = new ManageHiveJellyMessageEvent(args.Xeno, args.Gibbed, args.Name);
        _dialog.OpenInput(ent, Loc.GetString("rmc-jelly-citation-prompt"), ev, true, _commendation.CharacterLimit, _commendation.MinCharacterLimit, true);
    }

    private void OnManageHiveJellyMessage(Entity<ManageHiveComponent> ent, ref ManageHiveJellyMessageEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Xeno != null)
        {
            if (!TryGetEntity(args.Xeno, out var xeno))
                return;

            if (!CanAwardJellyPopup(ent.Owner, xeno.Value))
                return;

            if (!_commendation.ValidCommendation(ent.Owner, xeno.Value, args.Message))
                return;

            if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.JellyPlasmaCost))
                return;

            _commendation.GiveCommendation(ent.Owner, xeno.Value, Loc.GetString(args.Name), args.Message, CommendationType.Jelly);
        }
        else if (args.Gibbed != null)
        {
            if (!CanAwardJellyPopup(ent.Owner, args.Gibbed))
                return;

            if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.JellyPlasmaCost))
                return;

            _commendation.GiveCommendationByLastPlayerId(ent.Owner, args.Gibbed.LastPlayerId, args.Gibbed.Name, Loc.GetString(args.Name), args.Message, CommendationType.Jelly);
        }
        else
            return;

        _popup.PopupCursor(Loc.GetString("rmc-jelly-awarded"), ent, PopupType.Large);
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

        _dialog.OpenInput(manage, Loc.GetString("rmc-hivemanagement-provide-reason", ("name", Name(watched))), new ManageHiveDevolveMessageEvent(args.Choice));
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
            var msg = Loc.GetString("rmc-hivemanagement-queen-deevolving", ("reason", args.Message));
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
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-must-overwatch"), manage, manage, PopupType.MediumCaution);
            return false;
        }

        if (!TryComp(watchedId, out XenoDevolveComponent? devolve) ||
            devolve.DevolvesTo.Length == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-cant-be-devolved", ("name", Name(watchedId))), watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        if (!devolve.CanBeDevolvedByOther)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-cant-deevolve-larva"), watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        if (!_xenoPlasma.HasPlasmaPopup(manage.Owner, manage.Comp.DevolvePlasmaCost, false))
            return false;

        if (!_hive.FromSameHive(manage.Owner, watchedId))
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-cant-deevolve-other-hive"), watchedId, manage, PopupType.MediumCaution);
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
                _popup.PopupCursor(Loc.GetString("rmc-jelly-error-cant-give"), manage, PopupType.MediumCaution);

            return false;
        }

        if (manage.Comp2.Given >= _jelliesPerQueen)
        {
            if (popup)
                _popup.PopupCursor(Loc.GetString("rmc-jelly-error-limit-reached", ("given", manage.Comp2.Given), ("limit", _jelliesPerQueen)), manage, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    //For gibbed
    //We get these only from our own hive, so no need to track that
    private bool CanAwardJellyPopup(Entity<ManageHiveComponent?, CommendationGiverComponent?, HiveMemberComponent?, ActorComponent?> manage, GibbedXenoInfo target, bool popup = true)
    {
        if (!Resolve(manage, ref manage.Comp1, ref manage.Comp2, ref manage.Comp3, ref manage.Comp4, false))
            return false;

        if (Guid.Parse(target.LastPlayerId) == manage.Comp4.PlayerSession.UserId)
        {
            if (popup)
                _popup.PopupCursor(Loc.GetString("rmc-jelly-error-cant-give"), manage, PopupType.MediumCaution);
            return false;
        }

        if (manage.Comp2.Given >= _jelliesPerQueen)
        {
            if (popup)
                _popup.PopupCursor(Loc.GetString("rmc-jelly-error-limit-reached", ("given", manage.Comp2.Given), ("limit", _jelliesPerQueen)), manage, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    private void OnManageHiveTeams(Entity<ManageHiveComponent> manage, ref ManageHiveTeamsEvent args)
    {
        if (_net.IsClient)
            return;

        // Handled by HiveTeamSystem on the server via UI open
        // Have to raise a local event so the server HiveTeamSystem can open the UI
        var ev = new OpenHiveTeamsUIEvent();
        RaiseLocalEvent(manage.Owner, ref ev);
    }

    private bool CanSacrificeBurrowedPopup(Entity<ManageHiveComponent> user, out Entity<HiveComponent> hive)
    {
        hive = default;
        if (_hive.GetHive(user.Owner) is not { } userHive)
            return false;

        hive = userHive;
        if (hive.Comp.BurrowedLarva <= 0)
        {
            _popup.PopupCursor(Loc.GetString("rmc-hivemanagement-exchange-larva-not-enough"), user, PopupType.MediumCaution);
            return false;
        }

        var time = _burrowedLarvaSacrificeTime - _gameTicker.RoundDuration();
        if (time > TimeSpan.Zero)
        {
            var msg = Loc.GetString("rmc-hivemanagement-exchange-larva-need-minutes", ("minutes", (int) time.TotalMinutes));
            _popup.PopupCursor(msg, user, PopupType.MediumCaution);
            return false;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(user.Owner, user.Comp.SacrificeBurrowedLarvaForEvolutionCost, false))
            return false;

        return true;
    }
}
