using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Light;

public sealed class RMCTemporaryDisabledLightSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private readonly HashSet<Entity<PointLightComponent>> _nearbyLights = new();
    private readonly List<Entity<PointLightComponent>> _nearbyLightsList = new();
    private readonly List<Vector2> _keptLightPositions = new();

    private EntityQuery<ExpendableLightComponent> _expendableLightQuery;
    private EntityQuery<HandheldLightComponent> _handHeldLightQuery;

    private int _maxNearbyLights;
    private float _maxNearbyLightCheckRange;
    private TimeSpan _lightCheckInterval;
    private bool _maxNearbyLightsEnabled;

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        _expendableLightQuery = GetEntityQuery<ExpendableLightComponent>();
        _handHeldLightQuery = GetEntityQuery<HandheldLightComponent>();

        _maxNearbyLights = _config.GetCVar(RMCCVars.RMCLightningMaxAmountLightNearbyCount);
        _maxNearbyLightCheckRange = _config.GetCVar(RMCCVars.RMCLightningMaxAmountLightNearbyAreaSize);
        _lightCheckInterval = TimeSpan.FromSeconds(_config.GetCVar(RMCCVars.RMCLightningMaxAmountLightNearbyCheckIntervalSeconds));
        _maxNearbyLightsEnabled = _config.GetCVar(RMCCVars.RMCLightningMaxAmountLightNearbyEnabled);

        Subs.CVar(_config, RMCCVars.RMCLightningMaxAmountLightNearbyCount, SetMaxNearbyLights);
        Subs.CVar(_config, RMCCVars.RMCLightningMaxAmountLightNearbyAreaSize, SetMaxNearbyLightsCheckRange);
        Subs.CVar(_config, RMCCVars.RMCLightningMaxAmountLightNearbyCheckIntervalSeconds, SetLightCheckInterval);
        Subs.CVar(_config, RMCCVars.RMCLightningMaxAmountLightNearbyEnabled, SetMaxNearbyLightsEnabled);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_maxNearbyLightsEnabled)
            return;

        var time = _timing.CurTime;
        if (time < _nextUpdate)
            return;

        _nextUpdate = time + _lightCheckInterval;

        var query = EntityQueryEnumerator<PointLightComponent, ItemComponent>();
        while (query.MoveNext(out var uid, out var light, out _))
        {
            if (TryComp(uid, out RMCTemporaryDisabledLightComponent? disabledLight) && disabledLight.NextCheckAt > time)
                continue;

            _nearbyLights.Clear();
            _entityLookup.GetEntitiesInRange(_transform.GetMapCoordinates(uid), _maxNearbyLightCheckRange, _nearbyLights);
            _nearbyLights.RemoveWhere(ent => uid == ent.Owner ||
                                             _container.TryGetContainingContainer(ent.Owner, out var container) && container.OccludesLight ||
                                             (!_expendableLightQuery.TryComp(ent, out var expendable) || !expendable.Activated) &&
                                             (!_handHeldLightQuery.TryComp(ent, out var toggle) || !toggle.Activated));

            if (_nearbyLights.Count <= _maxNearbyLights)
            {
                RemComp<RMCTemporaryDisabledLightComponent>(uid);

                var enableLight =
                    _expendableLightQuery.TryComp(uid, out var expendable) && expendable.Activated ||
                    _handHeldLightQuery.TryComp(uid, out var toggle) && toggle.Activated;

                if (enableLight)
                    _pointLight.SetEnabled(uid, true, light);

                continue;
            }

            _keptLightPositions.Clear();
            _nearbyLightsList.Clear();
            _nearbyLightsList.AddRange(_nearbyLights);
            _nearbyLightsList.Sort((a, b) => a.Owner.Id.CompareTo(b.Owner.Id));

            var maxDistSq = _maxNearbyLightCheckRange * _maxNearbyLightCheckRange;
            foreach (var nearbyLight in _nearbyLightsList)
            {
                if (HasComp<RMCTemporaryDisabledLightComponent>(nearbyLight))
                    continue;

                var nearbyLightPosition = _transform.GetWorldPosition(nearbyLight.Owner);

                var nearbyKeptLights = 0;
                foreach (var keptLightPosition in _keptLightPositions)
                {
                    var distanceSquared = (nearbyLightPosition - keptLightPosition).LengthSquared();

                    if (!(distanceSquared < maxDistSq))
                        continue;

                    nearbyKeptLights++;
                }

                var shouldDisable = nearbyKeptLights >= _maxNearbyLights;
                if (shouldDisable)
                {
                    var disabled = EnsureComp<RMCTemporaryDisabledLightComponent>(nearbyLight);
                    disabled.NextCheckAt = time + _lightCheckInterval;

                    _pointLight.SetEnabled(nearbyLight, false, nearbyLight.Comp);
                }
                else
                {
                    _keptLightPositions.Add(nearbyLightPosition);
                    RemComp<RMCTemporaryDisabledLightComponent>(nearbyLight);

                    _pointLight.SetEnabled(nearbyLight, true, nearbyLight.Comp);
                }
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

    private void SetMaxNearbyLights(int amount)
    {
        _maxNearbyLights = Math.Max(0, amount);
    }

    private void SetMaxNearbyLightsCheckRange(float range)
    {
        _maxNearbyLightCheckRange = Math.Max(0.01f, range);
    }

    private void SetLightCheckInterval(float amount)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(0, amount));
        _lightCheckInterval = interval;
    }

    private void SetMaxNearbyLightsEnabled(bool enabled)
    {
        _maxNearbyLightsEnabled = enabled;
    }
}
