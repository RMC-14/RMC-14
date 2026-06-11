using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.ManageHive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Banish;

public sealed class XenoBanishSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedXenoWatchSystem _xenoWatch = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveBanishEvent>(OnManageHiveBanish);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveBanishXenoEvent>(OnManageHiveBanishXeno);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveBanishReasonEvent>(OnManageHiveBanishReason);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveReadmitEvent>(OnManageHiveReadmit);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveReadmitXenoEvent>(OnManageHiveReadmitXeno);
        SubscribeLocalEvent<ManageHiveComponent, ManageHiveReadmitConfirmEvent>(OnManageHiveReadmitConfirm);

        SubscribeLocalEvent<XenoBanishComponent, AttackAttemptEvent>(OnBanishedAttackAttempt, before: [typeof(XenoSystem)]);
        SubscribeLocalEvent<XenoBanishComponent, GettingAttackedAttemptEvent>(OnBanishedGettingAttacked, before: [typeof(XenoSystem)]);
    }

    private void OnManageHiveBanish(Entity<ManageHiveComponent> ent, ref ManageHiveBanishEvent args)
    {
        if (_net.IsClient)
            return;

        if (!CanBanishPopup(ent, out var watched) || watched == null)
            return;

        var rules = Loc.GetString("rmc-banish-rules");
        _dialog.OpenConfirmation(ent, Loc.GetString("rmc-banish-title"), rules, new ManageHiveBanishXenoEvent(GetNetEntity(watched.Value)));
    }

    private void OnManageHiveBanishXeno(Entity<ManageHiveComponent> ent, ref ManageHiveBanishXenoEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno) || !CanBanishPopup(ent, out var watched) || watched == null || xeno.Value != watched.Value)
            return;

        var msg = Loc.GetString("rmc-banish-confirm", ("name", Name(xeno.Value)));
        _dialog.OpenInput(ent, ent, msg, new ManageHiveBanishReasonEvent(args.Xeno, ""), true, 200);
    }

    private void OnManageHiveBanishReason(Entity<ManageHiveComponent> ent, ref ManageHiveBanishReasonEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno) || !CanBanishPopup(ent, out var watched) || watched == null || xeno.Value != watched.Value)
            return;

        if (string.IsNullOrWhiteSpace(args.Message))
        {
            _popup.PopupCursor(Loc.GetString("rmc-banish-no-reason"), ent, PopupType.MediumCaution);
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.BanishPlasmaCost))
            return;

        Banish(ent.Owner, xeno.Value, args.Message);
    }

    private void OnManageHiveReadmit(Entity<ManageHiveComponent> ent, ref ManageHiveReadmitEvent args)
    {
        if (_net.IsClient)
            return;

        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        if (hive.Comp.BanishedXenos.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-readmit-not-banished"), ent, ent, PopupType.MediumCaution);
            return;
        }

        var banishedList = new List<(EntityUid uid, string name, string reason, bool canReadmit, string? error)>();
        foreach (var banished in hive.Comp.BanishedXenos)
        {
            if (!TryComp<XenoBanishComponent>(banished, out var banishComp))
                continue;

            var elapsed = _timing.CurTime - banishComp.BanishedAt;
            var canReadmit = true;
            string? error = null;

            if (elapsed < ent.Comp.ReadmitMinTime)
            {
                var remaining = (int)(ent.Comp.ReadmitMinTime - elapsed).TotalMinutes + 1;
                error = $"(Wait {remaining} min)";
                canReadmit = false;
            }
            else if (_mobState.IsDead(banished))
            {
                error = "(Dead)";
                canReadmit = false;
            }

            var displayName = error != null ? $"{Name(banished)} - {banishComp.Reason} {error}" : $"{Name(banished)} - {banishComp.Reason}";
            banishedList.Add((banished, displayName, banishComp.Reason, canReadmit, error));
        }

        if (banishedList.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-readmit-not-banished"), ent, ent, PopupType.MediumCaution);
            return;
        }

        var options = new List<DialogOption>();
        foreach (var (uid, displayName, reason, canReadmit, error) in banishedList)
        {
            if (canReadmit)
                options.Add(new DialogOption(displayName, new ManageHiveReadmitXenoEvent(GetNetEntity(uid))));
            else
                options.Add(new DialogOption(displayName, null));
        }

        _dialog.OpenOptions(ent, Loc.GetString("rmc-readmit-title"), options);
    }

    private void OnManageHiveReadmitXeno(Entity<ManageHiveComponent> ent, ref ManageHiveReadmitXenoEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Xeno, out var xeno))
            return;

        if (!TryComp<XenoBanishComponent>(xeno, out var banish))
            return;

        var elapsed = _timing.CurTime - banish.BanishedAt;
        if (elapsed < ent.Comp.ReadmitMinTime || _mobState.IsDead(xeno.Value))
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.ReadmitPlasmaCost))
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

        if (!TryComp<XenoBanishComponent>(xeno, out var banish))
            return;

        var elapsed = _timing.CurTime - banish.BanishedAt;
        if (elapsed < ent.Comp.ReadmitMinTime || _mobState.IsDead(xeno.Value))
            return;

        Readmit(ent.Owner, xeno.Value);
    }

    private bool CanBanishPopup(Entity<ManageHiveComponent> manage, out EntityUid? watched)
    {
        watched = null;
        if (!_xenoWatch.TryGetWatched(manage.Owner, out var watchedId) || watchedId == manage.Owner)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-must-overwatch"), manage, manage, PopupType.MediumCaution);
            return false;
        }

        if (!HasComp<XenoComponent>(watchedId))
        {
            _popup.PopupEntity(Loc.GetString("rmc-banish-not-xeno"), watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        if (_mobState.IsCritical(watchedId))
        {
            _popup.PopupEntity(Loc.GetString("rmc-banish-crit"), watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        if (!_hive.FromSameHive(manage.Owner, watchedId))
        {
            _popup.PopupEntity(Loc.GetString("rmc-hivemanagement-cant-deevolve-other-hive"), watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        if (HasComp<XenoBanishComponent>(watchedId))
        {
            _popup.PopupEntity(Loc.GetString("rmc-banish-already-banished"), watchedId, manage, PopupType.MediumCaution);
            return false;
        }

        if (!_xenoPlasma.HasPlasmaPopup(manage.Owner, manage.Comp.BanishPlasmaCost, false))
            return false;

        watched = watchedId;
        return true;
    }

    private void Banish(EntityUid banisher, EntityUid banished, string reason)
    {
        var comp = EnsureComp<XenoBanishComponent>(banished);
        comp.BanishedAt = _timing.CurTime;
        comp.Reason = reason;

        if (_hive.GetHive(banished) is { } originalHive)
        {
            comp.OriginalHive = originalHive.Owner;
            var hiveComp = originalHive.Comp;
            hiveComp.BanishedXenos.Add(banished);
            Dirty(originalHive);
        }

        Dirty(banished, comp);

        _hive.SetHive(banished, null);

        _hive.ChangeBurrowedLarva(1);

        var ev = new XenoBanishedEvent(banisher, banished, reason);
        RaiseLocalEvent(ref ev);

        _adminLog.Add(LogType.RMCXenoBanish, $"{ToPrettyString(banisher)} banished {ToPrettyString(banished)} for: {reason}");
    }

    private void Readmit(EntityUid readmitter, EntityUid readmitted)
    {
        if (!TryComp<XenoBanishComponent>(readmitted, out var banishComp))
            return;

        if (_hive.GetHive(readmitted) is { } hive)
        {
            var hiveComp = hive.Comp;
            hiveComp.BanishedXenos.Remove(readmitted);
            Dirty(hive);
        }

        if (banishComp.OriginalHive is { } originalHiveId)
            _hive.SetHive(readmitted, originalHiveId);

        RemCompDeferred<XenoBanishComponent>(readmitted);

        var ev = new XenoReadmittedEvent(readmitter, readmitted);
        RaiseLocalEvent(ref ev);

        _adminLog.Add(LogType.RMCXenoReadmit, $"{ToPrettyString(readmitter)} readmitted {ToPrettyString(readmitted)}");
    }

    public bool IsBanished(EntityUid uid)
    {
        return HasComp<XenoBanishComponent>(uid);
    }

    private void OnBanishedAttackAttempt(Entity<XenoBanishComponent> banished, ref AttackAttemptEvent args)
    {
        if (banished.Comp.CanDamageHive || args.Target == null || banished.Comp.OriginalHive == null)
            return;

        if (_hive.IsMember(args.Target.Value, banished.Comp.OriginalHive.Value))
            args.Cancel();
    }

    private void OnBanishedGettingAttacked(Entity<XenoBanishComponent> banished, ref GettingAttackedAttemptEvent args)
    {
        // Banished xenos have no hive, so they can be attacked by anyone
    }
}
