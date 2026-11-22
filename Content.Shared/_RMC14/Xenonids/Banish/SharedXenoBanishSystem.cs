using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Content.Shared._RMC14.Xenonids.ManageHive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Banish;

public abstract class SharedXenoBanishSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly LarvaQueueSystem _larvaQueue = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveBanishEvent>(OnManageHiveBanish);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveBanishChooseXenoEvent>(OnManageHiveBanishChooseXeno);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveBanishReasonEvent>(OnManageHiveBanishReason);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveReadmitEvent>(OnManageHiveReadmit);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveReadmitXenoEvent>(OnManageHiveReadmitXeno);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveReadmitConfirmEvent>(OnManageHiveReadmitConfirm);

        SubscribeLocalEvent<XenoBanishComponent, AttackAttemptEvent>(OnBanishedAttackAttempt, before: [typeof(XenoSystem)]);

        SubscribeLocalEvent<HiveMemberComponent, MindAddedMessage>(OnHiveMemberMindAdded);
    }

    /// <summary>
    /// lessers and parasites are blocked for banished players, so getting into a living xenonid of the hive
    /// means they came back through the larva queue and aren't banished from it anymore
    /// </summary>
    private void OnHiveMemberMindAdded(Entity<HiveMemberComponent> ent, ref MindAddedMessage args)
    {
        if (_net.IsClient ||
            ent.Comp.Hive is not { } hive ||
            args.Mind.Comp.UserId is not { } player ||
            _mobState.IsDead(ent) ||
            !TryComp(hive, out HiveBanishedPlayersComponent? banished))
        {
            return;
        }

        banished.Players.Remove(player);
    }

    private void OnManageHiveBanish(Entity<ManageHiveComponent> ent, ref ManageHiveBanishEvent args)
    {
        if (_net.IsClient)
            return;

        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        if (!TryComp(ent, out ActorComponent? actor))
            return;

        try
        {
            var playTimes = _playtime.GetPlayTimes(actor.PlayerSession);
            if (!playTimes.TryGetValue(ent.Comp.PlayTime, out var time) ||
                time < ent.Comp.BanishRequiredTime)
            {
                _popup.PopupCursor(Loc.GetString("rmc-banish-error-not-enough-playtime", ("requiredHours", (int) ent.Comp.BanishRequiredTime.TotalHours)), ent, PopupType.LargeCaution);
                return;
            }
        }
        catch
        {
            _popup.PopupCursor(Loc.GetString("rmc-banish-error-not-enough-playtime", ("requiredHours", (int) ent.Comp.BanishRequiredTime.TotalHours)), ent, PopupType.LargeCaution);
            return;
        }

        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, ent.Comp.BanishPlasmaCost, false))
            return;

        var options = new List<DialogOption>();
        var query = EntityQueryEnumerator<XenoComponent, HiveMemberComponent>();
        while (query.MoveNext(out var uid, out _, out var member))
        {
            if (uid == ent.Owner)
                continue;

            if (member.Hive != hive.Owner)
                continue;

            if (HasComp<XenoBanishComponent>(uid))
                continue;

            if (_mobState.IsCritical(uid) || _mobState.IsDead(uid))
                continue;

            // Uses the mind instead of the session so disconnected players can still be banished
            if (_mind.GetMind(uid) is not { } mindId ||
                CompOrNull<MindComponent>(mindId)?.UserId is not { } player)
            {
                continue;
            }

            options.Add(new DialogOption(Name(uid), new ManageHiveBanishChooseXenoEvent(GetNetEntity(uid), player)));
        }

        if (options.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-banish-no-valid-targets"), ent, ent, PopupType.MediumCaution);
            return;
        }

        var rules = Loc.GetString("rmc-banish-rules");
        _dialog.OpenOptions(ent, Loc.GetString("rmc-banish-title"), options, rules);
    }

    private void OnManageHiveBanishChooseXeno(Entity<ManageHiveComponent> ent, ref ManageHiveBanishChooseXenoEvent args)
    {
        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        if (!CanBanishTargetPopup(ent, xeno.Value) ||
            !IsSamePlayerPopup(ent, xeno.Value, args.Player))
        {
            return;
        }

        var msg = Loc.GetString("rmc-banish-confirm", ("name", Name(xeno.Value)));
        _dialog.OpenInput(ent, ent, msg, new ManageHiveBanishReasonEvent(args.Xeno, args.Player), true, 200);
    }

    private void OnManageHiveBanishReason(Entity<ManageHiveComponent> ent, ref ManageHiveBanishReasonEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        if (!CanBanishTargetPopup(ent, xeno.Value) ||
            !IsSamePlayerPopup(ent, xeno.Value, args.Player) ||
            args.Player is not { } player)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(args.Message))
        {
            ErrorPopup(ent, Loc.GetString("rmc-banish-no-reason"));
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.BanishPlasmaCost, false))
            return;

        Banish(ent.Owner, xeno.Value, player, args.Message);
    }

    private void OnManageHiveReadmit(Entity<ManageHiveComponent> ent, ref ManageHiveReadmitEvent args)
    {
        if (_net.IsClient)
            return;

        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        var options = new List<DialogOption>();
        var query = EntityQueryEnumerator<XenoBanishComponent>();
        while (query.MoveNext(out var uid, out var banish))
        {
            if (banish.OriginalHive != hive.Owner)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            var text = Name(uid);
            if (GetReadmitTimeLeft(ent, banish) is { } timeLeft)
                text = Loc.GetString("rmc-readmit-option-wait", ("name", text), ("minutes", (int) Math.Ceiling(timeLeft.TotalMinutes)));

            // Still clickable while waiting so the queen gets told why it can't be done yet
            options.Add(new DialogOption(text, new ManageHiveReadmitXenoEvent(GetNetEntity(uid)), description: banish.Reason));
        }

        if (options.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-readmit-no-valid-targets"), ent, ent, PopupType.MediumCaution);
            return;
        }

        _dialog.OpenOptions(ent, Loc.GetString("rmc-readmit-title"), options);
    }

    private void OnManageHiveReadmitXeno(Entity<ManageHiveComponent> ent, ref ManageHiveReadmitXenoEvent args)
    {
        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        if (!CanReadmitTargetPopup(ent, xeno.Value))
            return;

        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, ent.Comp.ReadmitPlasmaCost, false, _net.IsServer))
            return;

        var msg = Loc.GetString("rmc-readmit-confirm", ("name", Name(xeno.Value)));
        _dialog.OpenConfirmation(ent, Loc.GetString("rmc-readmit-title"), msg, new ManageHiveReadmitConfirmEvent(args.Xeno));
    }

    private void OnManageHiveReadmitConfirm(Entity<ManageHiveComponent> ent, ref ManageHiveReadmitConfirmEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        if (!CanReadmitTargetPopup(ent, xeno.Value))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.ReadmitPlasmaCost, false))
            return;

        Readmit(ent.Owner, xeno.Value);
    }

    /// <summary>
    /// Shows an error above the queen. Only done by the server so it still shows when
    /// the client couldn't predict the target, and never shows twice.
    /// </summary>
    private void ErrorPopup(EntityUid manage, string msg)
    {
        if (_net.IsServer)
            _popup.PopupEntity(msg, manage, manage, PopupType.MediumCaution);
    }

    private bool CanBanishTargetPopup(Entity<ManageHiveComponent> manage, EntityUid target)
    {
        if (target == manage.Owner)
            return false;

        if (!HasComp<XenoComponent>(target))
        {
            ErrorPopup(manage, Loc.GetString("rmc-banish-not-xeno"));
            return false;
        }

        if (_mobState.IsCritical(target) || _mobState.IsDead(target))
        {
            ErrorPopup(manage, Loc.GetString("rmc-banish-crit"));
            return false;
        }

        if (!_hive.FromSameHive(manage.Owner, target))
        {
            ErrorPopup(manage, Loc.GetString("rmc-banish-different-hive"));
            return false;
        }

        if (HasComp<XenoBanishComponent>(target))
        {
            ErrorPopup(manage, Loc.GetString("rmc-banish-already-banished"));
            return false;
        }

        if (!_xenoPlasma.HasPlasmaPopup(manage.Owner, manage.Comp.BanishPlasmaCost, false, _net.IsServer))
            return false;

        return true;
    }

    /// <summary>
    /// Checks the xenonid is still controlled by the player it was when the banish list was opened,
    /// so a different player who took over the body in the meantime doesn't get banished instead.
    /// </summary>
    private bool IsSamePlayerPopup(EntityUid manage, EntityUid target, NetUserId? player)
    {
        if (_net.IsClient)
            return true;

        if (player != null &&
            _mind.GetMind(target) is { } mindId &&
            CompOrNull<MindComponent>(mindId)?.UserId == player)
        {
            return true;
        }

        ErrorPopup(manage, Loc.GetString("rmc-banish-player-changed"));
        return false;
    }

    private bool CanReadmitTargetPopup(Entity<ManageHiveComponent> manage, EntityUid target)
    {
        if (!TryComp(target, out XenoBanishComponent? banish))
        {
            ErrorPopup(manage, Loc.GetString("rmc-readmit-not-banished"));
            return false;
        }

        if (_mobState.IsDead(target))
        {
            ErrorPopup(manage, Loc.GetString("rmc-readmit-dead"));
            return false;
        }

        if (_hive.GetHive(manage.Owner) is not { } hive || banish.OriginalHive != hive.Owner)
        {
            ErrorPopup(manage, Loc.GetString("rmc-readmit-different-hive"));
            return false;
        }

        if (GetReadmitTimeLeft(manage, banish) is { } timeLeft)
        {
            ErrorPopup(manage, Loc.GetString("rmc-readmit-wait", ("minutes", (int) Math.Ceiling(timeLeft.TotalMinutes))));
            return false;
        }

        return true;
    }

    private TimeSpan? GetReadmitTimeLeft(Entity<ManageHiveComponent> manage, XenoBanishComponent banish)
    {
        var timeLeft = banish.BanishedAt + manage.Comp.ReadmitMinTime - _timing.CurTime;
        return timeLeft > TimeSpan.Zero ? timeLeft : null;
    }

    private void Banish(EntityUid banisher, EntityUid banished, NetUserId player, string reason)
    {
        var originalHive = _hive.GetHive(banished);
        var comp = EnsureComp<XenoBanishComponent>(banished);
        comp.BanishedAt = _timing.CurTime;
        comp.Reason = reason;
        comp.OriginalHive = originalHive?.Owner;
        Dirty(banished, comp);

        if (originalHive is { } hive)
        {
            // kept on the hive so leaving the body doesn't let the player rejoin as a lesser or parasite
            EnsureComp<HiveBanishedPlayersComponent>(hive).Players.Add(player);

            // lessers and parasites keep their place in the larva queue, so they'd be offered a larva straight away
            _larvaQueue.RemoveFromQueue(player, hive);
        }

        // Before removing them from the hive so the banished xenonid also gets the announcement
        OnBanished(banisher, (banished, comp));

        _hive.SetHive(banished, null);

        _adminLog.Add(LogType.RMCXenoBanish, $"{ToPrettyString(banisher)} banished {ToPrettyString(banished)} for: {reason}");
    }

    protected void Readmit(EntityUid readmitter, EntityUid readmitted)
    {
        if (!TryComp(readmitted, out XenoBanishComponent? banish))
            return;

        if (banish.OriginalHive is { } originalHive)
        {
            _hive.SetHive(readmitted, originalHive);

            if (TryComp(originalHive, out HiveBanishedPlayersComponent? banished) &&
                _mind.GetMind(readmitted) is { } mindId &&
                CompOrNull<MindComponent>(mindId)?.UserId is { } player)
            {
                banished.Players.Remove(player);
            }
        }

        OnReadmitted(readmitter, (readmitted, banish));

        RemCompDeferred<XenoBanishComponent>(readmitted);

        _adminLog.Add(LogType.RMCXenoReadmit, $"{ToPrettyString(readmitter)} readmitted {ToPrettyString(readmitted)}");
    }

    public bool IsBanishedFrom(NetUserId player, EntityUid hive)
    {
        return TryComp(hive, out HiveBanishedPlayersComponent? banished) && banished.Players.Contains(player);
    }

    /// <summary>
    /// checks the player isn't banished from the hive they would join by taking this lesser drone or parasite,
    /// telling them why if they are
    /// </summary>
    public bool CanJoinHivePopup(ICommonSession player, EntityUid joining)
    {
        if (_hive.GetHive(joining) is not { } hive ||
            !IsBanishedFrom(player.UserId, hive))
        {
            return true;
        }

        if (player.AttachedEntity is { } user)
            _popup.PopupEntity(Loc.GetString("rmc-banish-cannot-join", ("hive", Name(hive))), user, user, PopupType.MediumCaution);

        return false;
    }

    protected virtual void OnBanished(EntityUid banisher, Entity<XenoBanishComponent> banished)
    {
    }

    protected virtual void OnReadmitted(EntityUid readmitter, Entity<XenoBanishComponent> readmitted)
    {
    }

    private void OnBanishedAttackAttempt(Entity<XenoBanishComponent> banished, ref AttackAttemptEvent args)
    {
        if (args.Target == null || banished.Comp.OriginalHive == null)
            return;

        if (_hive.IsMember(args.Target.Value, banished.Comp.OriginalHive.Value))
            args.Cancel();
    }
}
