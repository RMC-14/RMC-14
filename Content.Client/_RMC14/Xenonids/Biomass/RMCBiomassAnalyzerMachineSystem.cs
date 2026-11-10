using Content.Shared._RMC14.Xenonids.Biomass;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassAnalyzerMachineSystem : SharedRMCBiomassAnalyzerMachineSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var time = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCBiomassAnalyzerMachineComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            if (!_sprite.TryGetLayer((uid, sprite), RMCBiomassAnalyzerVisualLayers.Animation, out var layer, false))
                continue;

            var state = comp.Animation switch
            {
                RMCBiomassAnalyzerVisualState.Processing => comp.ProcessingState,
                RMCBiomassAnalyzerVisualState.Printing => comp.PrintingState,
                _ => null,
            };

            if (state == null ||
                comp.AnimationStart is not { } start ||
                !IsMachinePowered(uid) ||
                layer.ActualRsi is not { } rsi ||
                !rsi.TryGetState(state, out var rsiState) ||
                time - start >= TimeSpan.FromSeconds(rsiState.AnimationLength))
            {
                if (layer.Visible)
                    _sprite.LayerSetVisible(layer, false);

                continue;
            }

            if (layer.State != state)
                _sprite.LayerSetRsiState(layer, state);

            if (layer.AutoAnimated)
                _sprite.LayerSetAutoAnimated(layer, false);

            _sprite.LayerSetAnimationTime(layer, (float) (time - start).TotalSeconds);

            if (!layer.Visible)
                _sprite.LayerSetVisible(layer, true);
        }
    }
}
