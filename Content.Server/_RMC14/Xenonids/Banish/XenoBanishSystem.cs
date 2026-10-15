using Content.Server._RMC14.Announce;
using Content.Server.Administration.Managers;
using Content.Shared._RMC14.Xenonids.Banish;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Banish;

public sealed class XenoBanishSystem : SharedXenoBanishSystem
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly XenoAnnounceSystem _announce = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoBanishComponent, MobStateChangedEvent>(OnBanishedMobStateChanged);
        SubscribeLocalEvent<XenoBanishComponent, GetVerbsEvent<Verb>>(OnBanishedGetVerbs);
    }

    private void OnBanishedGetVerbs(Entity<XenoBanishComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor) ||
            !_admin.HasAdminFlag(actor.PlayerSession, AdminFlags.Admin))
        {
            return;
        }

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("rmc-readmit-admin-verb"),
            Category = VerbCategory.Admin,
            Act = () => Readmit(user, ent),
            Impact = LogImpact.Medium,
        });
    }

    protected override void OnBanished(EntityUid banisher, Entity<XenoBanishComponent> banished)
    {
        var msg = Loc.GetString("rmc-banish-announcement", ("name", Name(banished)), ("reason", banished.Comp.Reason));
        _announce.AnnounceSameHiveDefaultSound(banisher, msg);

        if (TryComp(banished, out ActorComponent? actor))
        {
            var banishedMsg = Loc.GetString("rmc-banish-notification", ("reason", banished.Comp.Reason));
            _popup.PopupEntity(banishedMsg, banished, actor.PlayerSession, PopupType.LargeCaution);
        }
    }

    protected override void OnReadmitted(EntityUid readmitter, Entity<XenoBanishComponent> readmitted)
    {
        // Announced from the readmitted xenonid, since admins can readmit from outside the hive
        var msg = Loc.GetString("rmc-readmit-announcement", ("name", Name(readmitted)));
        _announce.AnnounceSameHiveDefaultSound(readmitted.Owner, msg);

        if (TryComp(readmitted, out ActorComponent? actor))
        {
            var readmittedMsg = Loc.GetString("rmc-readmit-notification");
            _popup.PopupEntity(readmittedMsg, readmitted, actor.PlayerSession, PopupType.Large);
        }
    }

    private void OnBanishedMobStateChanged(Entity<XenoBanishComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (ent.Comp.OriginalHive is not { } originalHive ||
            !TryComp(originalHive, out HiveComponent? hive))
        {
            return;
        }

        // When a banished xenonid dies, the original hive gets a burrowed larva to replace it
        _hive.ChangeBurrowedLarva((originalHive, hive), 1);
    }
}
