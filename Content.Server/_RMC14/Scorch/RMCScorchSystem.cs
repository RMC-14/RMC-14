using System.Linq;
using System.Numerics;
using Content.Server.Decals;
using Content.Shared.Decals;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Scorch;

public sealed class RMCScorchSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCScorchEffectOnSpawnComponent, ComponentStartup>(OnScorchEffectStartup);
    }

    private readonly Dictionary<string, string[]> _scorchDecals = new ();

    private void OnScorchEffectStartup(Entity<RMCScorchEffectOnSpawnComponent> ent, ref ComponentStartup args)
    {
        var decalTag = ent.Comp.DecalTag;
        if (!_scorchDecals.ContainsKey(decalTag))
        {
            CacheDecals(decalTag);
        }
        if (_scorchDecals[decalTag].Length == 0)
            return;

        var spawnProbability = ent.Comp.Probability;
        if (_random.NextFloat() > spawnProbability)
            return;

        //Check that tile limit for scorch decals hasn't been reached on the tile
        var tileLimit = ent.Comp.TileLimit;
        var gridUid = Transform(ent).GridUid;
        if (!gridUid.HasValue)
            return;
        var tileBounds = Box2.CenteredAround(Transform(ent).Coordinates.Offset(new Vector2(-0.5f, -0.5f)).Position, Vector2.One);
        var tileDecals = _decals.GetDecalsIntersecting((EntityUid)gridUid, tileBounds);
        //Only check the decal types we have cached, ignore other decals in the tile
        var tileCount = tileDecals.Count(x => _scorchDecals[decalTag].Contains(x.Decal.Id));
        if (tileCount >= tileLimit)
            return;

        //Spawn decal
        var decalId = _scorchDecals[decalTag][_random.Next(_scorchDecals[decalTag].Length)];
        //Decals spawn based on bottom left corner, if bigger decals are used the offset will have to change
        var decalOffset = ent.Comp.Scatter ? new Vector2(-1.0f + _random.NextFloat(), -1.0f + _random.NextFloat()) : new Vector2(-0.5f, -0.5f);
        var coords = Transform(ent).Coordinates.Offset(decalOffset);
        var rotationAngle = ent.Comp.RandomRotation ? _random.NextAngle() : Angle.FromDegrees(_random.Next(4) * 90);

        _decals.TryAddDecal(decalId, coords, out _, rotation: rotationAngle, cleanable: true);
    }

    private void CacheDecals(string decalTag)
    {
        _scorchDecals[decalTag] = _prototypeManager.EnumeratePrototypes<DecalPrototype>().Where(x => x.Tags.Contains(decalTag)).Select(x => x.ID).ToArray();
        if (_scorchDecals[decalTag].Length == 0)
            Log.Error($"Failed to get any decals for RMCScorchEffectOnSpawnComponent. Check that at least one decal has tag {decalTag}.");
    }
}
