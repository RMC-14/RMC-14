using Content.Client.Gameplay;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Item;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteFadeSystem : EntitySystem
{
    /*
    * If the player entity is obstructed under the specified components then it will drop the alpha for that entity
    * so the player is still visible.
    * Supports fading entire sprite or individual layers based on component configuration.
    */

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private List<(MapCoordinates Point, bool ExcludeBoundingBox)> _points = new();
    private readonly HashSet<RMCFadingSpriteComponent> _comps = new();
    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<RMCSpriteFadeComponent> _fadeQuery;
    private EntityQuery<RMCFadingSpriteComponent> _fadingQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _fadeQuery = GetEntityQuery<RMCSpriteFadeComponent>();
        _fadingQuery = GetEntityQuery<RMCFadingSpriteComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();

        SubscribeLocalEvent<RMCFadingSpriteComponent, ComponentRemove>(OnFadingRemove);
    }

    private void OnFadingRemove(Entity<RMCFadingSpriteComponent> entity, ref ComponentRemove args)
    {
        if (MetaData(entity).EntityLifeStage >= EntityLifeStage.Terminating || !TryComp<SpriteComponent>(entity, out var sprite))
            return;

        // Restore original sprite alpha
        _sprite.SetColor((entity, sprite), sprite.Color.WithAlpha(entity.Comp.OriginalAlpha));

        // Restore original layer alphas
        foreach (var (layerKey, originalAlpha) in entity.Comp.OriginalLayerAlphas)
        {
            if (_sprite.LayerMapTryGet((entity, sprite), layerKey, out var layerIndex, true))
            {
                var layer = sprite[layerIndex];
                _sprite.LayerSetColor((entity, sprite), layerIndex, layer.Color.WithAlpha(originalAlpha));
            }
        }
    }

    /// <summary>
    ///     Adds sprites to the fade set, and brings their alpha downwards
    /// </summary>
    private void FadeIn(float frameTime)
    {
        var player = _playerManager.LocalEntity;
        // ExcludeBoundingBox is set if we don't want to fade this sprite within the collision bounding boxes for the given POI
        _points.Clear();

        if (_uiManager.CurrentlyHovered is IViewportControl vp && _inputManager.MouseScreenPosition.IsValid)
        {
            _points.Add((vp.PixelToMap(_inputManager.MouseScreenPosition.Position), true));
        }

        if (TryComp(player, out TransformComponent? playerXform))
        {
            _points.Add((_transform.GetMapCoordinates(_playerManager.LocalEntity!.Value, xform: playerXform), false));
        }

        if (_stateManager.CurrentState is GameplayState state && _spriteQuery.TryGetComponent(player, out var playerSprite))
        {
            foreach (var (mapPos, excludeBB) in _points)
            {
                foreach (var ent in state.GetClickableEntities(mapPos, _eyeManager.CurrentEye, excludeFaded: false, ignoreInteractionTransparency: true))
                {
                    if (ent == player || !_fadeQuery.HasComponent(ent) || !_spriteQuery.TryGetComponent(ent, out var sprite) || sprite.DrawDepth < playerSprite.DrawDepth)
                        continue;


                    if (excludeBB && _fixturesQuery.TryComp(ent, out var body))
                    {
                        var transform = _physics.GetPhysicsTransform(ent);
                        var collided = false;
                        foreach (var fixture in body.Fixtures.Values)
                        {
                            if (!fixture.Hard) continue;
                            if (_fixtures.TestPoint(fixture.Shape, transform, mapPos.Position))
                            {
                                collided = true;
                                break;
                            }
                        }
                        if (collided) continue;
                    }

                    var fadeComponent = _fadeQuery.GetComponent(ent);
                    // If reactToMouse == false and this is a mouse hover - skip fade
                    if (excludeBB && fadeComponent.ReactToMouse == false)
                        continue;

                    if (!_fadingQuery.TryComp(ent, out var fading))
                    {
                        fading = AddComp<RMCFadingSpriteComponent>(ent);
                        fading.OriginalAlpha = sprite.Color.A;
                    }

                    _comps.Add(fading);

                    var targetAlpha = fadeComponent.TargetAlpha;
                    var changeRate = fadeComponent.ChangeRate;
                    var change = changeRate * frameTime;

                    if (fadeComponent.FadeLayers.Count > 0)
                    {
                        // Fade only specified layers
                        foreach (var layerKey in fadeComponent.FadeLayers)
                        {
                            if (_sprite.LayerMapTryGet((ent, sprite), layerKey, out var layerIndex, true))
                            {
                                var layer = sprite[layerIndex];

                                // Store original alpha if not already stored
                                if (!fading.OriginalLayerAlphas.ContainsKey(layerKey))
                                {
                                    fading.OriginalLayerAlphas[layerKey] = layer.Color.A;
                                }

                                var newAlpha = Math.Max(layer.Color.A - change, targetAlpha);
                                if (!layer.Color.A.Equals(newAlpha))
                                {
                                    _sprite.LayerSetColor((ent, sprite), layerIndex, layer.Color.WithAlpha(newAlpha));
                                }
                            }
                        }
                    }
                    else
                    {
                        // Fade entire sprite (original behavior)
                        var newColor = Math.Max(sprite.Color.A - change, targetAlpha);
                        if (!sprite.Color.A.Equals(newColor))
                        {
                            _sprite.SetColor((ent, sprite), sprite.Color.WithAlpha(newColor));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Bring sprites back up to their original alpha if they aren't in the fade set, and removes their fade component when done
    /// </summary>
    private void FadeOut(float frameTime)
    {
        var query = AllEntityQuery<RMCFadingSpriteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_comps.Contains(comp))
                continue;

            if (!_spriteQuery.TryGetComponent(uid, out var sprite))
                continue;

            if (!_fadeQuery.TryComp(uid, out var fadeComponent))
                continue;

            var changeRate = fadeComponent.ChangeRate;
            var change = changeRate * frameTime;

            if (fadeComponent.FadeLayers.Count > 0)
            {
                // Restore individual layers
                var allLayersRestored = true;

                foreach (var (layerKey, originalAlpha) in comp.OriginalLayerAlphas)
                {
                    if (_sprite.LayerMapTryGet((uid, sprite), layerKey, out var layerIndex, true))
                    {
                        var layer = sprite[layerIndex];
                        var newAlpha = Math.Min(layer.Color.A + change, originalAlpha);

                        if (!newAlpha.Equals(layer.Color.A))
                        {
                            _sprite.LayerSetColor((uid, sprite), layerIndex, layer.Color.WithAlpha(newAlpha));
                            allLayersRestored = false;
                        }
                    }
                }

                // Remove component only when all layers are restored
                if (allLayersRestored)
                {
                    RemCompDeferred<RMCFadingSpriteComponent>(uid);
                }
            }
            else
            {
                // Restore entire sprite (original behavior)
                var newColor = Math.Min(sprite.Color.A + change, comp.OriginalAlpha);

                if (!newColor.Equals(sprite.Color.A))
                {
                    _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(newColor));
                }
                else
                {
                    RemCompDeferred<RMCFadingSpriteComponent>(uid);
                }
            }
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        FadeIn(frameTime);
        FadeOut(frameTime);

        _comps.Clear();
    }
}
