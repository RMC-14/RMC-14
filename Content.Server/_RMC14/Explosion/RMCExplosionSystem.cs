using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Scorch;
using Content.Server.Decals;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Voicelines;
using Content.Shared.Coordinates;
using Content.Shared.Decals;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Sticky;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Explosion;

public sealed class RMCExplosionSystem : SharedRMCExplosionSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly HumanoidVoicelinesSystem _humanoidVoicelines = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    private readonly Dictionary<string, ProtoId<DecalPrototype>[]> _scorchDecalsByTag = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<ActiveTimerTriggerEvent>(OnActiveTimerTrigger);

        SubscribeLocalEvent<CMVocalizeTriggerComponent, ActiveTimerTriggerEvent>(OnVocalizeTriggered);

        SubscribeLocalEvent<RMCExplosiveDeleteComponent, EntityStuckEvent>(OnExplosiveDeleteWallsStuck);

        SubscribeLocalEvent<RMCScorchEffectComponent, CMExplosiveTriggeredEvent>(OnExplosionEffectTriggered);

        SubscribeLocalEvent<RandomTimerTriggerComponent, ExaminedEvent>(OnRandomTimerTriggerExamined);

        CacheDecals("RMCScorch");
        CacheDecals("RMCScorchSmall");
    }

    private void OnActiveTimerTrigger(ref ActiveTimerTriggerEvent ev)
    {
        var rmcEv = new RMCActiveTimerTriggerEvent();
        RaiseLocalEvent(ev.Triggered, ref rmcEv);
    }

    private void OnTrigger(TriggerEvent ev)
    {
        var rmcEv = new RMCTriggerEvent(ev.User, ev.Handled);
        RaiseLocalEvent(ev.Triggered, ref rmcEv);
        ev.Handled = rmcEv.Handled;
    }

    private void OnVocalizeTriggered(Entity<CMVocalizeTriggerComponent> ent, ref ActiveTimerTriggerEvent args)
    {
        SpawnAttachedTo(ent.Comp.Effect, ent.Owner.ToCoordinates());

        if (args.User is not { } user)
            return;

        var popup = Loc.GetString(ent.Comp.UserPopup, ("used", ent.Owner));
        _popup.PopupEntity(popup, user, user, PopupType.LargeCaution);

        popup = Loc.GetString(ent.Comp.OthersPopup, ("user", user), ("used", ent.Owner));
        _popup.PopupEntity(popup, user, Filter.PvsExcept(user), true, ent.Comp.PopupType);

        var gender = CompOrNull<HumanoidAppearanceComponent>(user)?.Sex ?? Sex.Unsexed;
        if (!ent.Comp.Sounds.TryGetValue(gender, out var sound))
            return;

        var filter = Filter.Pvs(user).RemoveWhere(s => !_humanoidVoicelines.ShouldPlayVoiceline(user, s));
        if (filter.Count == 0)
            return;

        _audio.PlayEntity(sound, filter, user, true);
    }

    private void OnExplosiveDeleteWallsStuck(Entity<RMCExplosiveDeleteComponent> ent, ref EntityStuckEvent args)
    {
        _trigger.HandleTimerTrigger(ent, args.User, ent.Comp.Delay, ent.Comp.BeepInterval, null, ent.Comp.BeepSound);
    }

    private void OnExplosionEffectTriggered(Entity<RMCScorchEffectComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        if (ent.Comp.Radius > 0)
        {
            if (!_rmcMap.TryGetTileRefForEnt(Transform(ent).Coordinates, out var grid, out var tile))
                return;

            var centerDecals = GetDecals(ent.Comp.CenterDecalTag);
            var edgeDecals = GetDecals(ent.Comp.EdgeDecalTag);
            if (centerDecals.Length == 0 || edgeDecals.Length == 0)
                return;

            var radius = Math.Max(1, ent.Comp.Radius);
            var centerRadius = Math.Max(0, ent.Comp.CenterRadius);
            var centerRadiusSq = centerRadius * centerRadius;
            var radiusSq = radius * radius;
            var center = tile.GridIndices;

            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var distSq = dx * dx + dy * dy;
                    if (distSq > radiusSq)
                        continue;

                    var indices = new Vector2i(center.X + dx, center.Y + dy);
                    var coords = _map.GridTileToLocal(tile.GridUid, grid, indices);
                    var decalCoords = coords.Offset(new Vector2(-0.5f, -0.5f));
                    var decals = distSq <= centerRadiusSq ? centerDecals : edgeDecals;
                    var decalId = decals[_random.Next(decals.Length)];
                    var rotation = ent.Comp.RandomRotation ? _random.NextAngle() : Angle.FromDegrees(_random.Next(4) * 90);
                    _decals.TryAddDecal(decalId, decalCoords, out _, rotation: rotation, cleanable: true);
                }
            }
        }
        else
        {
            var decals = GetDecals(ent.Comp.CenterDecalTag);
            if (decals.Length == 0)
                return;

            var count = Math.Max(1, ent.Comp.Count);
            // Decals spawn based on bottom left corner, if bigger decals are used the offset will have to change
            var baseCoords = Transform(ent).Coordinates.Offset(new Vector2(-0.5f, -0.5f));
            var scatterRadius = Math.Max(0f, ent.Comp.ScatterRadius);

            for (var i = 0; i < count; i++)
            {
                var decalId = decals[_random.Next(decals.Length)];
                var offset = Vector2.Zero;
                if (scatterRadius > 0f)
                {
                    var angle = _random.NextAngle();
                    var dist = _random.NextFloat() * scatterRadius;
                    offset = angle.ToVec() * dist;
                }

                var coords = baseCoords.Offset(offset);
                var rotation = ent.Comp.RandomRotation ? _random.NextAngle() : Angle.FromDegrees(_random.Next(4) * 90);
                _decals.TryAddDecal(decalId, coords, out _, rotation: rotation, cleanable: true);
            }
        }
    }

    private void OnRandomTimerTriggerExamined(Entity<RandomTimerTriggerComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RandomTimerTriggerComponent)))
        {
            args.PushMarkup($"[color=cyan]This will have a random timer between {ent.Comp.Min} and {ent.Comp.Max} seconds on use![/color]");
        }
    }

    public override void QueueExplosion(
        MapCoordinates epicenter,
        string typeId,
        float totalIntensity,
        float slope,
        float maxTileIntensity,
        EntityUid? cause,
        float tileBreakScale = 1f,
        int maxTileBreak = int.MaxValue,
        bool canCreateVacuum = true,
        bool addLog = true)
    {
        _explosion.QueueExplosion(
            epicenter,
            typeId,
            totalIntensity,
            slope,
            maxTileIntensity,
            cause,
            tileBreakScale,
            maxTileBreak,
            canCreateVacuum,
            addLog
        );
    }

    public override void TriggerExplosive(EntityUid uid,
        bool delete = true,
        float? totalIntensity = null,
        float? radius = null,
        EntityUid? user = null)
    {
        _explosion.TriggerExplosive(uid, null, delete, totalIntensity, radius, user);
    }

    private ProtoId<DecalPrototype>[] GetDecals(string decalTag)
    {
        if (_scorchDecalsByTag.TryGetValue(decalTag, out var cached))
            return cached;

        CacheDecals(decalTag);
        return _scorchDecalsByTag.TryGetValue(decalTag, out var decals) ? decals : Array.Empty<ProtoId<DecalPrototype>>();
    }

    private void CacheDecals(string decalTag)
    {
        var decals = _prototypeManager.EnumeratePrototypes<DecalPrototype>()
            .Where(x => x.Tags.Contains(decalTag))
            .Select(x => new ProtoId<DecalPrototype>(x.ID))
            .ToArray();

        _scorchDecalsByTag[decalTag] = decals;
        if (decals.Length == 0)
            Log.Error($"Failed to get any decals for RMCScorchEffectComponent. Check that at least one decal has tag {decalTag}.");
    }
}
