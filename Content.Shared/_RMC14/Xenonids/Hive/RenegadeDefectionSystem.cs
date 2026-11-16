using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Xenonids.IffTag;
using Content.Shared.Damage;
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
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly RMCXenoIffTagSystem _iffTag = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly ProtoId<NpcFactionPrototype> MarineFaction = "UNMC";

    private readonly List<EntityUid> _toResolve = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<HiveFactionAllyChangedEvent>(OnFactionAllyChanged);
        SubscribeLocalEvent<RenegadeDefectionPendingComponent, RenegadeDefectionChosenEvent>(OnChosen);
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
        while (tags.MoveNext(out var uid, out _, out _))
        {
            if (uid == hive.CurrentQueen)
                continue;

            if (HasComp<RenegadeDefectionPendingComponent>(uid))
                continue;

            if (_hive.GetHive(uid) is not { } memberHive || memberHive.Owner != args.Hive)
                continue;

            if (!_iffTag.HasFaction(uid, iffFaction))
                continue;

            OfferDefection(uid, args.Faction);
        }
    }

    private void OfferDefection(EntityUid xeno, ProtoId<NpcFactionPrototype> faction)
    {
        var pending = EnsureComp<RenegadeDefectionPendingComponent>(xeno);
        pending.BrokenFaction = faction;
        pending.ExpiresAt = _timing.CurTime + pending.ChoiceTimeout;

        var options = new List<DialogOption>
        {
            new("Obey the Queen", new RenegadeDefectionChosenEvent(false)),
            new("Obey the talls", new RenegadeDefectionChosenEvent(true)),
        };

        _dialog.OpenOptions(xeno,
            "Alliance broken!",
            options,
            $"Our Queen has broken the alliance with the {faction.Id}. The device inside our carapace begins to suppress our connection with the Hive. Do we remove it and follow the Queen?");
    }

    private void OnChosen(Entity<RenegadeDefectionPendingComponent> ent, ref RenegadeDefectionChosenEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Defect)
            Defect(ent);
        else
            Stay(ent);
    }

    private void Defect(Entity<RenegadeDefectionPendingComponent> xeno)
    {
        var brokenFaction = xeno.Comp.BrokenFaction;
        RemComp<RenegadeDefectionPendingComponent>(xeno);

        if (!_hive.TryGetHiveBySlot(HiveSlots.Renegade, out var renegadeHive))
        {
            Log.Error($"Tried to defect {ToPrettyString(xeno.Owner)} to the Renegade hive, but no Renegade hive slot exists.");
            return;
        }

        _hive.SetHive(xeno.Owner, renegadeHive);

        _popup.PopupEntity("You lost the connection with your Hive. Now you have no Queen, only your masters.", xeno, xeno, PopupType.LargeCaution);
        _popup.PopupEntity($"{Name(xeno)} seems to have lost its connection to its Hive!", xeno, Filter.PvsExcept(xeno), true, PopupType.MediumCaution);

        if (brokenFaction == MarineFaction)
            _marineAnnounce.AnnounceARES(null, "The advanced IFF Xenonid tagging technology has detected hostile intentions and successfully suppressed the psychic link of a lifeform. The collaborative lifeform is given designation Renegade. Full cooperation is to be expected.");
    }

    private void Stay(Entity<RenegadeDefectionPendingComponent> xeno)
    {
        var ripDamage = xeno.Comp.RipDamage;
        RemComp<RenegadeDefectionPendingComponent>(xeno);

        if (!_iffTag.RemoveTag(xeno.Owner))
            return;

        _damageable.TryChangeDamage(xeno.Owner, ripDamage, true, origin: xeno.Owner);

        _popup.PopupEntity("You rip the IFF tag out of your own carapace! For the Hive!", xeno, xeno, PopupType.LargeCaution);
        _popup.PopupEntity($"{Name(xeno)} rips out its IFF tag!", xeno, Filter.PvsExcept(xeno), true, PopupType.MediumCaution);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        _toResolve.Clear();

        var query = EntityQueryEnumerator<RenegadeDefectionPendingComponent>();
        while (query.MoveNext(out var uid, out var pending))
        {
            if (time < pending.ExpiresAt && _ui.IsUiOpen(uid, DialogUiKey.Key))
                continue;

            _toResolve.Add(uid);
        }

        foreach (var uid in _toResolve)
        {
            if (!TryComp(uid, out RenegadeDefectionPendingComponent? pending))
                continue;

            _ui.CloseUi(uid, DialogUiKey.Key);
            Stay((uid, pending));
        }
    }
}
