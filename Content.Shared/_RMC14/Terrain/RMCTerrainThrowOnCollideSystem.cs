using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Terrain;

public sealed class RMCTerrainThrowOnCollideSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<(EntityUid Terrain, EntityUid Other), TimeSpan> _cooldowns = new();
    private readonly List<(EntityUid Terrain, EntityUid Other)> _expired = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCTerrainThrowOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    public override void Update(float frameTime)
    {
        if (_cooldowns.Count == 0)
            return;

        var time = _timing.CurTime;
        _expired.Clear();
        foreach (var (key, until) in _cooldowns)
        {
            if (time >= until)
                _expired.Add(key);
        }

        foreach (var key in _expired)
        {
            _cooldowns.Remove(key);
        }
    }

    private void OnStartCollide(Entity<RMCTerrainThrowOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsClient ||
            _whitelist.IsWhitelistFail(ent.Comp.Whitelist, args.OtherEntity))
        {
            return;
        }

        var key = (ent.Owner, args.OtherEntity);
        var time = _timing.CurTime;
        if (_cooldowns.TryGetValue(key, out var next) && time < next)
            return;

        var direction = ent.Comp.Direction?.ToVec() ?? _transform.GetWorldRotation(ent).ToWorldVec();
        if (direction == default)
            return;

        _cooldowns[key] = time + ent.Comp.Cooldown;
        _throwing.TryThrow(args.OtherEntity, direction, ent.Comp.ThrowSpeed, animated: false, playSound: false);
    }
}
