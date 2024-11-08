using System.Linq;
using System.Numerics;
using Content.Server.Decals;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared.Decals;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Dropship.Weapon;

public sealed class DropshipWeaponSystem : SharedDropshipWeaponSystem
{
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private string[] _scorchDecals = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCScorchEffectOnSpawnComponent, ComponentStartup>(OnScorchEffectStartup);

        CacheDecals();
    }

    private void OnScorchEffectStartup(Entity<RMCScorchEffectOnSpawnComponent> ent, ref ComponentStartup args)
    {
        if (_scorchDecals.Length == 0) return;

        var spawnProbability = ent.Comp.Probability;
        if (_random.NextFloat() > spawnProbability) return;

        //Check that tile limit for scorch decals hasn't been reached on the tile
        var tileLimit = ent.Comp.TileLimit;
        var gridUid = Transform(ent).GridUid;
        if (!gridUid.HasValue) return;
        var tileBounds = Box2.CenteredAround(Transform(ent).Coordinates.Offset(new Vector2(-0.5f, -0.5f)).Position, Vector2.One);
        var tileDecals = _decals.GetDecalsIntersecting((EntityUid)gridUid, tileBounds);
        //Only check the decal types we have cached, ignore other decals in the tile
        var tileCount = tileDecals.Count(x => _scorchDecals.Contains(x.Decal.Id));
        if (tileCount >= tileLimit) return;

        //Spawn decal
        var decalId = _scorchDecals[_random.Next(_scorchDecals.Length)];
        //Decals spawn based on bottom left corner, if bigger decals are used the offset will have to change
        var coords = Transform(ent).Coordinates.Offset(new Vector2(-1.0f + _random.NextFloat(), -1.0f + _random.NextFloat()));
        _decals.TryAddDecal(decalId, coords, out _, rotation: _random.NextAngle(), cleanable: true);
    }

    protected override void AddPvs(Entity<DropshipTerminalWeaponsComponent> terminal, Entity<ActorComponent?> actor)
    {
        base.AddPvs(terminal, actor);

        if (terminal.Comp.Target is not { } target)
            return;

        if (!Resolve(actor, ref actor.Comp, false))
            return;

        _viewSubscriber.AddViewSubscriber(target, actor.Comp.PlayerSession);
    }

    protected override void RemovePvs(Entity<DropshipTerminalWeaponsComponent> terminal, Entity<ActorComponent?> actor)
    {
        base.AddPvs(terminal, actor);

        if (terminal.Comp.Target is not { } target)
            return;

        if (!Resolve(actor, ref actor.Comp, false))
            return;

        _viewSubscriber.RemoveViewSubscriber(target, actor.Comp.PlayerSession);
    }
    private void CacheDecals()
    {
        _scorchDecals = _prototypeManager.EnumeratePrototypes<DecalPrototype>().Where(x => x.Tags.Contains("RMCScorchSmall")).Select(x => x.ID).ToArray();
        if (_scorchDecals.Length == 0)
            Log.Error("Failed to get any decals for RMCScorchEffectOnSpawnComponent. Check that at least one decal has tag RMCScorchSmall.");
    }
}
