using Content.Shared._RMC14.Weapons.Ranged;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged;

public sealed class BulletholeVisualizerSystem : VisualizerSystem<BulletholeVisualsComponent>
{
    private const string BulletholeRsiPath = "/Textures/_RMC14/Effects/bulletholes.rsi";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BulletholeVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, BulletholeVisualsComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

    }

    protected override void OnAppearanceChange(EntityUid uid, BulletholeVisualsComponent component, ref AppearanceChangeEvent args)
    {
        Logger.Debug($"Picked up appearance change on BholeVisuals: {BulletholeRsiPath}");

        if (args.Sprite is not { } sprite)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, BulletholeVisualLayers.State, out var state, args.Component))
            return;

        if (!sprite.LayerMapTryGet(BulletholeVisualsLayers.Bullethole, out var layer))
            layer = sprite.LayerMapReserveBlank(BulletholeVisualsLayers.Bullethole);


        Logger.Debug($"Setting sprite to state {state}");
        var valid = !string.IsNullOrWhiteSpace(state);

        args.Sprite.LayerSetVisible(BulletholeVisualsLayers.Bullethole, valid);

        if (valid)
        {
            args.Sprite.LayerSetRSI(BulletholeVisualsLayers.Bullethole, BulletholeRsiPath);
            args.Sprite.LayerSetState(BulletholeVisualsLayers.Bullethole, state);
        }
    }
}

public enum BulletholeVisualsLayers: byte
{
    Bullethole
}
