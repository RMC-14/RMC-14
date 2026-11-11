using Content.Client.Chemistry.Visualizers;
using Content.Shared._RMC14.Smoke;
using Content.Shared.Smoking;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Smoke;

public sealed class RMCSmokeFadeVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly RMCSmokeFadeSystem _smokeFade = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSmokeFadeComponent, AppearanceChangeEvent>(OnAppearanceChange, after: [typeof(SmokeVisualizerSystem)]);
    }

    private void OnAppearanceChange(Entity<RMCSmokeFadeComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is { } sprite && HasComp<SmokeVisualsComponent>(ent))
            UpdateAlpha((ent, ent.Comp, sprite));
    }

    public override void FrameUpdate(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCSmokeFadeComponent, SmokeVisualsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var fade, out _, out var sprite))
        {
            if (time < fade.FadeStart)
                continue;

            UpdateAlpha((uid, fade, sprite));
        }
    }

    private void UpdateAlpha(Entity<RMCSmokeFadeComponent, SpriteComponent> ent)
    {
        if (!_appearance.TryGetData<Color>(ent, SmokeVisuals.Color, out var color))
            return;

        _sprite.SetColor((ent, ent.Comp2), color.WithAlpha(_smokeFade.GetAlpha(ent.Comp1, _timing.CurTime)));
    }
}
