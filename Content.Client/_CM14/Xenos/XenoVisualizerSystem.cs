using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Rest;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using XenoComponent = Content.Shared._CM14.Xenos.XenoComponent;

namespace Content.Client._CM14.Xenos;

public sealed class XenoVisualizerSystem : VisualizerSystem<XenoComponent>
{
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
                ClearDrawDepth((uid, component, sprite));

                if (rsi.TryGetState("crit", out _))
                    sprite.LayerSetState(layer, "crit");
                break;
            case MobState.Dead:
                SetDrawDepth((uid, component, sprite));

                if (rsi.TryGetState("dead", out _))
                    sprite.LayerSetState(layer, "dead");
                break;
            default:
                ClearDrawDepth((uid, component, sprite));

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
    }

    private void SetDrawDepth(Entity<XenoComponent, SpriteComponent> ent)
    {
        var (_, xeno, sprite) = ent;
        if (sprite.DrawDepth > (int) DrawDepth.DeadMobs)
        {
            xeno.OriginalDrawDepth = sprite.DrawDepth;
            sprite.DrawDepth = (int) DrawDepth.DeadMobs;
        }
    }

    private void ClearDrawDepth(Entity<XenoComponent, SpriteComponent> ent)
    {
        var (_, xeno, sprite) = ent;
        if (xeno.OriginalDrawDepth != null)
        {
            sprite.DrawDepth = xeno.OriginalDrawDepth.Value;
            xeno.OriginalDrawDepth = null;
        }
    }
}
