using Content.Shared._RMC14.Basketball;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Basketball;

public sealed class RMCBasketballVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBasketballScoreboardComponent, ComponentStartup>(OnScoreboardStartup);
        SubscribeLocalEvent<RMCBasketballScoreboardComponent, AfterAutoHandleStateEvent>(OnScoreboardState);

        SubscribeLocalEvent<RMCBasketballResetComponent, ComponentStartup>(OnResetStartup);
        SubscribeLocalEvent<RMCBasketballResetComponent, AfterAutoHandleStateEvent>(OnResetState);
    }

    private void OnScoreboardStartup(Entity<RMCBasketballScoreboardComponent> ent, ref ComponentStartup args)
    {
        UpdateScoreboard(ent);
    }

    private void OnScoreboardState(Entity<RMCBasketballScoreboardComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateScoreboard(ent);
    }

    private void OnResetStartup(Entity<RMCBasketballResetComponent> ent, ref ComponentStartup args)
    {
        UpdateReset(ent);
    }

    private void OnResetState(Entity<RMCBasketballResetComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateReset(ent);
    }

    private void UpdateScoreboard(Entity<RMCBasketballScoreboardComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite) ||
            !_sprite.LayerMapTryGet((ent, sprite), RMCBasketballScoreboardLayers.LeftTens, out var leftTens, true) ||
            !_sprite.LayerMapTryGet((ent, sprite), RMCBasketballScoreboardLayers.LeftOnes, out var leftOnes, true) ||
            !_sprite.LayerMapTryGet((ent, sprite), RMCBasketballScoreboardLayers.RightTens, out var rightTens, true) ||
            !_sprite.LayerMapTryGet((ent, sprite), RMCBasketballScoreboardLayers.RightOnes, out var rightOnes, true))
        {
            return;
        }

        _sprite.LayerSetRsiState((ent, sprite), leftTens, GetDigitState(ent.Comp.LeftScore / 10, 'a'));
        _sprite.LayerSetRsiState((ent, sprite), leftOnes, GetDigitState(ent.Comp.LeftScore % 10, 'b'));
        _sprite.LayerSetRsiState((ent, sprite), rightTens, GetDigitState(ent.Comp.RightScore / 10, 'c'));
        _sprite.LayerSetRsiState((ent, sprite), rightOnes, GetDigitState(ent.Comp.RightScore % 10, 'd'));
    }

    private void UpdateReset(Entity<RMCBasketballResetComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite) ||
            !_sprite.LayerMapTryGet((ent, sprite), RMCBasketballResetLayers.Base, out var layer, true))
        {
            return;
        }

        _sprite.LayerSetRsiState((ent, sprite), layer, GetResetState(ent.Comp.Pressed));
    }

    public static string GetDigitState(int digit, char position)
    {
        return $"s{Math.Clamp(digit, 0, 9)}{position}";
    }

    public static string GetResetState(bool pressed)
    {
        return pressed ? "launcheract" : "launcherbtt";
    }
}
