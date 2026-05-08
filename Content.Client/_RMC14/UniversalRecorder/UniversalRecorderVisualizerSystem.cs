using Content.Shared._RMC14.UniversalRecorder;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.UniversalRecorder;

public sealed class UniversalRecorderVisualizerSystem : VisualizerSystem<UniversalRecorderComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UniversalRecorderComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<UniversalRecorderComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        UpdateState(ent, sprite, TryComp(ent, out AppearanceComponent? appearance) ? appearance : null);
    }

    protected override void OnAppearanceChange(EntityUid uid, UniversalRecorderComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateState(uid, args.Sprite, args.Component);
    }

    private void UpdateState(EntityUid uid, SpriteComponent sprite, AppearanceComponent? appearance)
    {
        if (!SpriteSystem.LayerMapTryGet((uid, sprite), UniversalRecorderVisualLayers.Base, out var layer, false))
            return;

        SpriteSystem.LayerSetAutoAnimated((uid, sprite), layer, true);

        if (appearance == null ||
            !AppearanceSystem.TryGetData(uid, UniversalRecorderVisuals.State, out UniversalRecorderVisualState state, appearance))
        {
            return;
        }

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
