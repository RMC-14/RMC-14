using Content.Client.Gameplay;
using Content.Shared._RMC14.Sprite;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCSpriteFadeSystem : EntitySystem
{
    /*
    * If the player entity is obstructed under the specified components then it will drop the alpha for that entity
    * so the player is still visible.
    * TODO RMC14: Sprite fading by layers
    */

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

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

        SubscribeLocalEvent<RMCFadingSpriteComponent, ComponentShutdown>(OnFadingShutdown);
    }

    private void OnFadingShutdown(EntityUid uid, RMCFadingSpriteComponent component, ComponentShutdown args)
    {
        if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(component.OriginalAlpha));
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
                foreach (var ent in state.GetClickableEntities(mapPos, excludeFaded: false))
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

                    if (!_fadingQuery.TryComp(ent, out var fading))
                    {
                        fading = AddComp<RMCFadingSpriteComponent>(ent);
                        fading.OriginalAlpha = sprite.Color.A;
                    }

                    _comps.Add(fading);

                    var fadeComponent = _fadeQuery.GetComponent(ent);
                    // If reactToMouse == false and this is a mouse hover - skip fade
                    if (excludeBB && fadeComponent.ReactToMouse == false)
                        continue;

                    var targetAlpha = fadeComponent.TargetAlpha;
                    var changeRate = fadeComponent.ChangeRate;
                    var change = changeRate * frameTime;
                    var newColor = Math.Max(sprite.Color.A - change, targetAlpha);

                    if (!sprite.Color.A.Equals(newColor))
                    {
                        _sprite.SetColor((ent, sprite), sprite.Color.WithAlpha(newColor));
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

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        FadeIn(frameTime);
        FadeOut(frameTime);

        _comps.Clear();
    }
}
