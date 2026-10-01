using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Movement;

public sealed class RMCImmobileActionSystem : EntitySystem
{
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    private EntityQuery<XenoComponent> _xenoQuery;

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _userFactions = new();
    private readonly HashSet<EntProtoId<IFFFactionComponent>> _pusherFactions = new();

    public override void Initialize()
    {
        _xenoQuery = GetEntityQuery<XenoComponent>();
    }

    public bool BlocksPush(EntityUid user, EntityUid pusher)
    {
        if (!IsSameFaction(user, pusher))
            return false;

        _size.TryGetSize(user, out var userSize);
        _size.TryGetSize(pusher, out var pusherSize);
        return pusherSize <= userSize;
    }

    private bool IsSameFaction(EntityUid user, EntityUid pusher)
    {
        var userXeno = _xenoQuery.HasComp(user);
        var pusherXeno = _xenoQuery.HasComp(pusher);
        if (userXeno || pusherXeno)
            return userXeno && pusherXeno && _hive.FromSameHive(user, pusher);

        _gunIFF.TryGetFactions(user, _userFactions);
        _gunIFF.TryGetFactions(pusher, _pusherFactions);
        if (_userFactions.Count == 0 && _pusherFactions.Count == 0)
            return true;

        return _userFactions.Overlaps(_pusherFactions);
    }
}
