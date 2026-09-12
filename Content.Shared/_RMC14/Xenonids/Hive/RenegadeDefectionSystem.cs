using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Xenonids.IffTag;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Hive;

public sealed class RenegadeDefectionSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly RMCXenoIffTagSystem _iffTag = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageTypePrototype> TagRipDamage = "Blunt";
    private static readonly TimeSpan ChoiceTimeout = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        SubscribeLocalEvent<HiveFactionAllyChangedEvent>(OnFactionAllyChanged);
        SubscribeNetworkEvent<RenegadeDefectionChoiceEvent>(OnChoiceReceived);
    }

    private void OnFactionAllyChanged(ref HiveFactionAllyChangedEvent args)
    {
        if (args.Allied || _net.IsClient)
            return;

        if (!TryComp(args.Hive, out HiveSlotComponent? slot) || slot.Position != HiveSlots.Corrupted)
            return;

        if (!IffNpcFactionMap.NpcToIff.TryGetValue(args.Faction, out var iffFaction))
            return;

        if (!TryComp(args.Hive, out HiveComponent? hive))
            return;

        var tags = EntityQueryEnumerator<RMCXenoIffTagComponent, ActorComponent>();
        while (tags.MoveNext(out var uid, out _, out var actor))
        {
            if (uid == hive.CurrentQueen)
                continue;

            if (HasComp<RenegadeDefectionPendingComponent>(uid))
                continue;

            if (_hive.GetHive(uid) is not { } memberHive || memberHive.Owner != args.Hive)
                continue;

            if (!_iffTag.HasFaction(uid, iffFaction))
                continue;

            OfferDefection((uid, actor), args.Faction);
        }
    }

    private void OfferDefection(Entity<ActorComponent> xeno, ProtoId<NpcFactionPrototype> faction)
    {
        var pending = EnsureComp<RenegadeDefectionPendingComponent>(xeno.Owner);
        pending.BrokenFaction = faction;

        var expiresAt = _timing.CurTime + ChoiceTimeout;
        RaiseNetworkEvent(new RenegadeDefectionOfferEvent
        {
            Xeno = GetNetEntity(xeno.Owner),
            Faction = faction.Id,
            ExpiresAt = expiresAt.TotalSeconds,
        }, xeno.Comp.PlayerSession);

        Timer.Spawn(ChoiceTimeout, () => ResolveTimeout(xeno.Owner));
    }

    private void ResolveTimeout(EntityUid xeno)
    {
        if (_net.IsClient || !HasComp<RenegadeDefectionPendingComponent>(xeno))
            return;

        if (TryComp(xeno, out ActorComponent? actor))
        {
            RaiseNetworkEvent(new RenegadeDefectionOfferExpiredEvent
            {
                Xeno = GetNetEntity(xeno),
            }, actor.PlayerSession);
        }

        Stay(xeno);
    }

    private void OnChoiceReceived(RenegadeDefectionChoiceEvent ev, EntitySessionEventArgs args)
    {
        if (_net.IsClient)
            return;

        var xeno = GetEntity(ev.Xeno);

        if (!TryComp(xeno, out RenegadeDefectionPendingComponent? pending))
            return;

        if (!TryComp(xeno, out ActorComponent? actor) || actor.PlayerSession != args.SenderSession)
            return;

        if (ev.Defect)
            Defect(xeno, pending.BrokenFaction);
        else
            Stay(xeno);
    }

    private void Defect(EntityUid xeno, ProtoId<NpcFactionPrototype> brokenFaction)
    {
        RemComp<RenegadeDefectionPendingComponent>(xeno);

        if (!_hive.TryGetHiveBySlot(HiveSlots.Renegade, out var renegadeHive))
        {
            Log.Error($"Tried to defect {ToPrettyString(xeno)} to the Renegade hive, but no Renegade hive slot exists.");
            return;
        }

        _hive.SetHive(xeno, renegadeHive);

        _popup.PopupEntity(Loc.GetString("rmc-xeno-renegade-defect-self"), xeno, xeno, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-renegade-defect-others", ("xeno", xeno)), xeno, Filter.PvsExcept(xeno), true, PopupType.MediumCaution);

        if (brokenFaction.Id == "UNMC")
            _marineAnnounce.AnnounceARES(null, Loc.GetString("rmc-xeno-renegade-defect-announce"));
    }

    private void Stay(EntityUid xeno)
    {
        RemComp<RenegadeDefectionPendingComponent>(xeno);

        if (!_iffTag.RemoveTag(xeno))
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict[TagRipDamage] = 50;
        _damageable.TryChangeDamage(xeno, damage, true, origin: xeno);

        _popup.PopupEntity(Loc.GetString("rmc-xeno-renegade-stay-self"), xeno, xeno, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-renegade-stay-others", ("xeno", xeno)), xeno, Filter.PvsExcept(xeno), true, PopupType.MediumCaution);
    }
}
