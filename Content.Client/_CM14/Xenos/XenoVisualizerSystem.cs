using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Rest;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using XenoComponent = Content.Shared._CM14.Xenos.XenoComponent;

namespace Content.Client._CM14.Xenos;

public sealed class XenoVisualizerSystem : VisualizerSystem<XenoComponent>
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, GetDrawDepthEvent>(OnXenoGetDrawDepth);
    }

    private void OnXenoGetDrawDepth(Entity<XenoComponent> ent, ref GetDrawDepthEvent args)
    {
        if (_mobState.IsDead(ent))
        {
            if (args.DrawDepth > DrawDepth.DeadMobs)
            {
                args.DrawDepth = DrawDepth.DeadMobs;
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, XenoComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite is not { BaseRSI: { } rsi } ||
            !sprite.LayerMapTryGet(XenoVisualLayers.Base, out var layer))
        {
            return;
        }

        var state = CompOrNull<MobStateComponent>(uid)?.CurrentState;

        switch (state)
        {
            case MobState.Critical:
                if (rsi.TryGetState("crit", out _))
                    sprite.LayerSetState(layer, "crit");
                break;
            case MobState.Dead:
                if (rsi.TryGetState("dead", out _))
                    sprite.LayerSetState(layer, "dead");
                break;
            default:
                if (args.AppearanceData.TryGetValue(XenoVisualLayers.Base, out var resting) &&
                    resting is XenoRestState.Resting)
                {
                    if (rsi.TryGetState("sleeping", out _))
                        sprite.LayerSetState(layer, "sleeping");
                    break;
                }

                if (rsi.TryGetState("alive", out _))
                    sprite.LayerSetState(layer, "alive");
                break;
        }

        UpdateDrawDepth((uid, sprite));
    }

    public void UpdateDrawDepth(Entity<SpriteComponent?> xeno)
    {
        if (!Resolve(xeno, ref xeno.Comp))
            return;

        var ev = new GetDrawDepthEvent(DrawDepth.Mobs);
        RaiseLocalEvent(xeno, ref ev);

        xeno.Comp.DrawDepth = (int) ev.DrawDepth;
    }
}
