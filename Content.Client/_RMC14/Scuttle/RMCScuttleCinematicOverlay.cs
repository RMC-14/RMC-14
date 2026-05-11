using Content.Shared._RMC14.Rules;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Scuttle;

public sealed class RMCScuttleCinematicOverlay : Overlay
{
    private static readonly ResPath SpritePath = new("/Textures/_RMC14/Interface/scuttle_explosion.rsi");
    private static readonly SpriteSpecifier.Rsi IntroShip = new(SpritePath, "intro_ship");
    private static readonly SpriteSpecifier.Rsi IntroNuke = new(SpritePath, "intro_nuke");
    private static readonly SpriteSpecifier.Rsi ShipDestroyed = new(SpritePath, "ship_destroyed");
    private static readonly SpriteSpecifier.Rsi SummaryDestroyed = new(SpritePath, "summary_destroyed");

    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SpriteSystem _sprite;
    private readonly TimeSpan _startedAt;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public TimeSpan Duration { get; }

    public bool Finished => _timing.CurTime >= _startedAt + Duration;

    public RMCScuttleCinematicOverlay(TimeSpan startedAt, TimeSpan duration)
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entity.System<SpriteSystem>();
        _startedAt = startedAt;
        Duration = duration;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return !Finished && args.ViewportControl != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var elapsed = _timing.CurTime - _startedAt;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        var texture = GetFrame(elapsed);
        var viewport = args.ViewportBounds;
        var viewportSize = new Vector2(viewport.Width, viewport.Height);
        if (viewportSize.X <= 0 || viewportSize.Y <= 0)
            return;

        var handle = args.ScreenHandle;
        handle.DrawRect(viewport, Color.Black);

        var textureSize = new Vector2(texture.Width, texture.Height);
        var scale = MathF.Min(viewportSize.X / textureSize.X, viewportSize.Y / textureSize.Y);
        var drawSize = textureSize * scale;
        var center = new Vector2(
            (viewport.Left + viewport.Right) * 0.5f,
            (viewport.Top + viewport.Bottom) * 0.5f);
        var rect = UIBox2.FromDimensions(center - drawSize * 0.5f, drawSize);
        handle.DrawTextureRect(texture, rect);
    }

    private Texture GetFrame(TimeSpan elapsed)
    {
        var introShip = RMCScuttleCinematicTiming.GetIntroShipDuration(Duration);
        var introNuke = RMCScuttleCinematicTiming.GetIntroNukeDuration(Duration);
        var summary = RMCScuttleCinematicTiming.GetSummaryDuration(Duration);
        var destroyedStart = introShip + introNuke;
        var summaryStart = Duration - summary;

        if (elapsed < introShip)
            return _sprite.GetFrame(IntroShip, TimeSpan.Zero);

        if (elapsed < destroyedStart)
            return _sprite.GetFrame(IntroNuke, elapsed - introShip, loop: false);

        if (elapsed >= summaryStart)
            return _sprite.GetFrame(SummaryDestroyed, TimeSpan.Zero);

        return _sprite.GetFrame(ShipDestroyed, elapsed - destroyedStart, loop: false);
    }
}
