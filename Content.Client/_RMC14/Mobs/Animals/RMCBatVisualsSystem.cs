using Content.Client.DamageState;
using Content.Shared._RMC14.Mobs.Animals;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.Mobs.Animals;

public sealed class RMCBatVisualsSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBatHangingComponent, AppearanceChangeEvent>(
            OnAppearanceChange,
            after: [typeof(GenericVisualizerSystem)]);
    }

    private void OnAppearanceChange(Entity<RMCBatHangingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var hanging = _appearance.TryGetData<bool>(
            ent.Owner,
            RMCBatVisuals.Hanging,
            out var value,
            args.Component) && value;

        _sprite.LayerSetRenderingStrategy(
            (ent.Owner, args.Sprite),
            DamageStateVisualLayers.Base,
            hanging ? LayerRenderingStrategy.Default : LayerRenderingStrategy.UseSpriteStrategy);

        args.Sprite.NoRotation = !hanging;
    }
}
