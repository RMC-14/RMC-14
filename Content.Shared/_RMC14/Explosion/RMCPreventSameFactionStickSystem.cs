using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Sticky;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

public sealed class RMCPreventSameFactionStickSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _xenoHive = default!;

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _targetFactions = new();
    private readonly HashSet<EntProtoId<IFFFactionComponent>> _userFactions = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPreventSameFactionStickComponent, AttemptEntityStickEvent>(OnAttemptStick);
    }

    private void OnAttemptStick(Entity<RMCPreventSameFactionStickComponent> ent, ref AttemptEntityStickEvent args)
    {
        if (args.Cancelled || !HasComp<MobStateComponent>(args.Target))
            return;

        if (!IsSameFaction(args.User, args.Target))
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString(ent.Comp.Popup), args.User, args.User, PopupType.MediumCaution);
    }

    private bool IsSameFaction(EntityUid user, EntityUid target)
    {
        if (_xenoHive.FromSameHive(user, target))
            return true;

        if (HasComp<HiveMemberComponent>(user) || HasComp<HiveMemberComponent>(target))
            return false;

        _userFactions.Clear();
        _targetFactions.Clear();

        var userFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, _userFactions);
        RaiseLocalEvent(user, ref userFactionEvent);

        if (_userFactions.Count == 0)
            return false;

        var targetFactionEvent = new GetIFFFactionEvent(SlotFlags.IDCARD, _targetFactions);
        RaiseLocalEvent(target, ref targetFactionEvent);

        return _targetFactions.Count != 0 && _userFactions.Overlaps(_targetFactions);
    }
}
