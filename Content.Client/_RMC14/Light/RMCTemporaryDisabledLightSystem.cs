using Content.Shared.Item;
using Content.Shared.Light.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Light;

public sealed class RMCTemporaryDisabledLightSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly TimeSpan LightCheckInterval = TimeSpan.FromSeconds(1);

    private const int MaxNearbyLights = 8;
    private const float MaxNearbyLightCheckRange = 2.5f;

    private readonly HashSet<Entity<PointLightComponent>> _nearbyLights = new();
    private readonly List<Entity<PointLightComponent>> _nearbyLightsList = new();

    private EntityQuery<ExpendableLightComponent> _expendableLightQuery;
    private EntityQuery<HandheldLightComponent> _handHeldLightQuery;

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        _expendableLightQuery = GetEntityQuery<ExpendableLightComponent>();
        _handHeldLightQuery = GetEntityQuery<HandheldLightComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        if (time < _nextUpdate)
            return;

        _nextUpdate = time + LightCheckInterval;

        var query = EntityQueryEnumerator<PointLightComponent, ItemComponent>();
        while (query.MoveNext(out var uid, out var light, out _))
        {
            if (TryComp(uid, out RMCTemporaryDisabledLightComponent? disabledLight) && disabledLight.NextCheckAt > time)
                continue;

            _nearbyLights.Clear();
            _entityLookup.GetEntitiesInRange(_transform.GetMapCoordinates(uid), MaxNearbyLightCheckRange, _nearbyLights);
            _nearbyLights.RemoveWhere(ent => _container.TryGetContainingContainer(ent.Owner, out var container) && container.OccludesLight ||
                                             (!_expendableLightQuery.TryComp(ent, out var expendable) || !expendable.Activated) &&
                                             (!_handHeldLightQuery.TryComp(ent, out var toggle) || !toggle.Activated));

            if (_nearbyLights.Count <= MaxNearbyLights)
            {
                RemComp<RMCTemporaryDisabledLightComponent>(uid);

                var enableLight =
                    _expendableLightQuery.TryComp(uid, out var expendable) && expendable.Activated ||
                    _handHeldLightQuery.TryComp(uid, out var toggle) && toggle.Activated;

                if (enableLight)
                    _pointLight.SetEnabled(uid, true, light);

                continue;
            }

            _nearbyLightsList.Clear();
            _nearbyLightsList.AddRange(_nearbyLights);
            _nearbyLightsList.Sort((a, b) => a.Owner.Id.CompareTo(b.Owner.Id));

            for (var i = 0; i < _nearbyLightsList.Count; i++)
            {
                var enabledLight = _nearbyLightsList[i];
                var shouldDisable = i < MaxNearbyLights;

                if (!shouldDisable)
                    continue;

                var disabled = EnsureComp<RMCTemporaryDisabledLightComponent>(enabledLight);
                disabled.NextCheckAt = time + LightCheckInterval;

                _pointLight.SetEnabled(enabledLight, false, enabledLight.Comp);
            }
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<RMCTemporaryDisabledLightComponent, PointLightComponent>();

        while (query.MoveNext(out var uid, out _, out var light))
        {
            if (light.Enabled)
                _pointLight.SetEnabled(uid, false, light);
        }
    }
}
