using Content.Client.Light.Visualizers;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Hallucination;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private readonly List<FlickerLight> _lights = new();
    private readonly HashSet<Entity<PointLightComponent>> _lightLookup = new();
    private RMCHallucinationLightsEvent? _lightsEvent;
    private TimeSpan _lightsStart;
    private TimeSpan _lightsEnd;
    private bool _allowFlashing;

    private void InitializeDanger()
    {
        SubscribeNetworkEvent<RMCHallucinationLightsEvent>(OnLights);

        Subs.CVar(_config, RMCCVars.RMCAllowFlashingLights, v => _allowFlashing = v, true);
    }

    private void OnLights(RMCHallucinationLightsEvent ev)
    {
        if (_player.LocalEntity is not { } local)
            return;

        RestoreLights();
        TrackOwner();

        var time = _timing.CurTime;
        _lightsEvent = ev;
        _lightsStart = time;
        _lightsEnd = time + ev.Duration;

        _lightLookup.Clear();
        _lookup.GetEntitiesInRange(Transform(local).Coordinates, ev.Range, _lightLookup);
        foreach (var (uid, light) in _lightLookup)
        {
            if (!light.Enabled)
                continue;

            var flicker = new FlickerLight(uid, light.Radius) { NextToggle = time + _random.Next(ev.OnMin, ev.OnMax) };
            if (TryComp(uid, out SpriteComponent? sprite))
            {
                if (_sprite.LayerMapTryGet((uid, sprite), ExpendableLightVisualLayers.Glow, out var glow, false))
                    flicker.GlowLayer = (glow, sprite[glow].Visible);

                if (TryComp(uid, out PoweredLightVisualsComponent? visuals) &&
                    _sprite.LayerMapTryGet((uid, sprite), PoweredLightLayers.Base, out var bulb, false))
                {
                    var states = visuals.SpriteStateMap;
                    if (states.TryGetValue(PoweredLightState.On, out var onState) &&
                        states.TryGetValue(PoweredLightState.Off, out var offState) &&
                        sprite[bulb] is SpriteComponent.Layer { State.Name: { } current } &&
                        current == onState)
                    {
                        flicker.Bulb = (bulb, onState, offState);
                    }
                }
            }

            _lights.Add(flicker);
        }
    }

    private void UpdateDanger(TimeSpan time)
    {
        if (_lights.Count == 0 || _lightsEvent is not { } settings)
            return;

        if (time >= _lightsEnd)
        {
            RestoreLights();
            return;
        }

        var progress = (float) ((time - _lightsStart) / (_lightsEnd - _lightsStart));
        for (var i = _lights.Count - 1; i >= 0; i--)
        {
            var flicker = _lights[i];
            if (TerminatingOrDeleted(flicker.Uid) || !_light.TryGetLight(flicker.Uid, out var light))
            {
                _lights.RemoveAt(i);
                continue;
            }

            if (LightChangedExternally(flicker, light))
            {
                RestoreLight(flicker, light);
                _lights.RemoveAt(i);
                continue;
            }

            if (_allowFlashing)
            {
                if (time >= flicker.NextToggle)
                {
                    flicker.On = !flicker.On;
                    flicker.NextToggle = time + (flicker.On
                        ? _random.Next(settings.OnMin, settings.OnMax)
                        : _random.Next(settings.OffMin, settings.OffMax));
                }

                SetLightOn(flicker, light, flicker.On);
            }
            else
            {
                if (flicker.SetEnabled == false)
                    SetLightOn(flicker, light, true);

                var radius = flicker.Radius * (1f - settings.BrownoutDepth * MathF.Sin(MathF.PI * progress));
                _light.SetRadius(flicker.Uid, radius, light);
                flicker.SetRadius = radius;
            }
        }
    }

    private static bool LightChangedExternally(FlickerLight flicker, SharedPointLightComponent light)
    {
        return (flicker.SetEnabled is { } enabled && light.Enabled != enabled) ||
               (flicker.SetRadius is { } radius && !MathHelper.CloseTo(light.Radius, radius));
    }

    private void SetLightOn(FlickerLight flicker, SharedPointLightComponent light, bool on)
    {
        if (light.Enabled != on)
            _light.SetEnabled(flicker.Uid, on, light);

        flicker.SetEnabled = on;
        SetFlickerSprite(flicker, on);
    }

    private void SetFlickerSprite(FlickerLight flicker, bool on)
    {
        if (!TryComp(flicker.Uid, out SpriteComponent? sprite))
            return;

        if (flicker.GlowLayer is var (glow, glowVisible) && glowVisible)
            _sprite.LayerSetVisible((flicker.Uid, sprite), glow, on);

        if (flicker.Bulb is var (bulb, onState, offState))
            _sprite.LayerSetRsiState((flicker.Uid, sprite), bulb, on ? onState : offState);
    }

    private void RestoreLight(FlickerLight flicker, SharedPointLightComponent light)
    {
        if (flicker.SetEnabled == false)
        {
            if (light.Enabled)
                SetFlickerSprite(flicker, true);
            else
                SetLightOn(flicker, light, true);
        }

        if (flicker.SetRadius is { } radius && MathHelper.CloseTo(light.Radius, radius))
            _light.SetRadius(flicker.Uid, flicker.Radius, light);
    }

    private void RestoreLights()
    {
        foreach (var flicker in _lights)
        {
            if (TerminatingOrDeleted(flicker.Uid) || !_light.TryGetLight(flicker.Uid, out var light))
                continue;

            RestoreLight(flicker, light);
        }

        _lights.Clear();
        _lightsEvent = null;
    }

    private void ClearDanger()
    {
        RestoreLights();
    }

    private sealed class FlickerLight(EntityUid uid, float radius)
    {
        public readonly EntityUid Uid = uid;
        public readonly float Radius = radius;
        public (int Layer, bool Visible)? GlowLayer;
        public (int Layer, string On, string Off)? Bulb;
        public bool On = true;
        public TimeSpan NextToggle;
        public bool? SetEnabled;
        public float? SetRadius;
    }
}
