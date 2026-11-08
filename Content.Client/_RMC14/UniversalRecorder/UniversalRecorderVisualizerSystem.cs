using Content.Shared._RMC14.UniversalRecorder;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.UniversalRecorder;

public sealed class UniversalRecorderVisualizerSystem : VisualizerSystem<AppearanceComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AppearanceComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null ||
            !AppearanceSystem.TryGetData(uid, UniversalRecorderVisuals.State, out UniversalRecorderVisualState state, component))
        {
            return;
        }

        UpdateState(uid, args.Sprite, state);
    }

    private void UpdateState(EntityUid uid, SpriteComponent sprite, UniversalRecorderVisualState state)
    {
        if (!SpriteSystem.LayerMapTryGet((uid, sprite), UniversalRecorderVisualLayers.Base, out var layer, false))
            return;

        SpriteSystem.LayerSetAutoAnimated((uid, sprite), layer, true);
        SpriteSystem.LayerSetRsiState((uid, sprite), layer, state switch
        {
            UniversalRecorderVisualState.Empty => "taperecorder_empty",
            UniversalRecorderVisualState.Idle => "taperecorder_idle",
            UniversalRecorderVisualState.Recording => "taperecorder_recording",
            UniversalRecorderVisualState.Playing => "taperecorder_playing",
            _ => "taperecorder_idle",
        });
    }
}
