using Content.Client.Light.Components;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Client.Light.EntitySystems;

public sealed class ExpendableLightVisualizerSystem : VisualizerSystem<ExpendableLightComponent>
{
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly LightBehaviorSystem _lightBehavior = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendableLightComponent, ComponentShutdown>(OnLightShutdown);
    }

    private void OnLightShutdown(EntityUid uid, ExpendableLightComponent component, ComponentShutdown args)
    {
        component.PlayingStream = _audioSystem.Stop(component.PlayingStream);
    }

    protected override void OnAppearanceChange(EntityUid uid, ExpendableLightComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, ExpendableLightVisuals.Behavior, out var lightBehaviourID, args.Component)
            && TryComp<LightBehaviourComponent>(uid, out var lightBehaviour))
        {
            _lightBehavior.StopLightBehaviour((uid, lightBehaviour));

            if (!string.IsNullOrEmpty(lightBehaviourID))
            {
                _lightBehavior.StartLightBehaviour((uid, lightBehaviour), lightBehaviourID);
            }
            else if (TryComp<PointLightComponent>(uid, out var light))
            {
                _pointLightSystem.SetEnabled(uid, false, light);
            }
        }

        if (!AppearanceSystem.TryGetData<ExpendableLightState>(uid, ExpendableLightVisuals.State, out var state, args.Component))
            return;

        switch (state)
        {
            case ExpendableLightState.Lit:
                _audioSystem.Stop(comp.PlayingStream);
                comp.PlayingStream = _audioSystem.PlayPvs(
                    comp.LoopedSound, uid)?.Entity;

                if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), ExpendableLightVisualLayers.Overlay, out var layerIdx, true))
                {
                    if (!string.IsNullOrWhiteSpace(comp.IconStateLit))
                        SpriteSystem.LayerSetRsiState((uid, args.Sprite), layerIdx, comp.IconStateLit);
                    if (!string.IsNullOrWhiteSpace(comp.SpriteShaderLit))
                        args.Sprite.LayerSetShader(layerIdx, comp.SpriteShaderLit);
                    else
                        args.Sprite.LayerSetShader(layerIdx, null, null);
                    if (comp.GlowColorLit.HasValue)
                        SpriteSystem.LayerSetColor((uid, args.Sprite), layerIdx, comp.GlowColorLit.Value);
                    SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIdx, true);
                }
                _popups.PopupEntity("appearanceChanged", uid);
                //if (comp.AltGlowColorLit.HasValue)
                // {
                //    _popups.PopupEntity(comp.AltGlowColorLit.Value.ToString(), uid);
                //    SpriteSystem.LayerSetColor((uid, args.Sprite), ExpendableLightVisualLayers.Glow, comp.AltGlowColorLit.Value);
                //}
                if(AppearanceSystem.TryGetData<Color>(uid, ExpendableLightVisuals.Color, out var color, args.Component))
                {
                    _popups.PopupEntity("asdfniasdfn9uiasnfi", uid);
                    SpriteSystem.LayerSetColor((uid, args.Sprite), ExpendableLightVisualLayers.Glow, color);
                }
                if (comp.GlowColorLit.HasValue)
                {
                    _popups.PopupEntity("asdfniasdfn9uiasnfi", uid);
                    SpriteSystem.LayerSetColor((uid, args.Sprite), ExpendableLightVisualLayers.Glow, comp.GlowColorLit.Value);
                }else
                    _popups.PopupEntity("no value", uid);
                SpriteSystem.LayerSetVisible((uid, args.Sprite), ExpendableLightVisualLayers.Glow, true);

                break;
            case ExpendableLightState.Dead:
                comp.PlayingStream = _audioSystem.Stop(comp.PlayingStream);
                if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), ExpendableLightVisualLayers.Overlay, out layerIdx, true))
                {
                    if (!string.IsNullOrWhiteSpace(comp.IconStateSpent))
                        SpriteSystem.LayerSetRsiState((uid, args.Sprite), layerIdx, comp.IconStateSpent);
                    if (!string.IsNullOrWhiteSpace(comp.SpriteShaderSpent))
                        args.Sprite.LayerSetShader(layerIdx, comp.SpriteShaderSpent);
                    else
                        args.Sprite.LayerSetShader(layerIdx, null, null);
                }

                SpriteSystem.LayerSetVisible((uid, args.Sprite), ExpendableLightVisualLayers.Glow, false);
                break;
        }
    }
}
