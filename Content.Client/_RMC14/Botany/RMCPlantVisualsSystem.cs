using Content.Shared._RMC14.Botany;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Botany;

public enum RMCPlantLayers : byte
{
    Plant,
    Flower,
}

public sealed class RMCPlantVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPlantComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCPlantComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCPlantComponent, AfterAutoHandleStateEvent>(OnPlantState);
        SubscribeLocalEvent<RMCPlantGrowthComponent, AfterAutoHandleStateEvent>(OnGrowthState);
        SubscribeLocalEvent<RMCPlantTraitsComponent, AfterAutoHandleStateEvent>(OnTraitsState);
        SubscribeLocalEvent<RMCPlantTraitFlowersComponent, ComponentStartup>(OnFlowersStartup);
        SubscribeLocalEvent<RMCPlantTraitFlowersComponent, ComponentShutdown>(OnFlowersShutdown);
        SubscribeLocalEvent<RMCPlantTraitFlowersComponent, AfterAutoHandleStateEvent>(OnFlowersState);
    }

    private void OnInit(Entity<RMCPlantComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerMapReserve((ent, sprite), RMCPlantLayers.Plant);
        _sprite.LayerSetVisible((ent, sprite), RMCPlantLayers.Plant, false);

        _sprite.LayerMapReserve((ent, sprite), RMCPlantLayers.Flower);
        _sprite.LayerSetVisible((ent, sprite), RMCPlantLayers.Flower, false);
    }

    private void OnStartup(Entity<RMCPlantComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent);
    }

    private void OnPlantState(Entity<RMCPlantComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    private void OnGrowthState(Entity<RMCPlantGrowthComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    private void OnTraitsState(Entity<RMCPlantTraitsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    private void OnFlowersStartup(Entity<RMCPlantTraitFlowersComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent);
    }

    private void OnFlowersShutdown(Entity<RMCPlantTraitFlowersComponent> ent, ref ComponentShutdown args)
    {
        UpdateSprite(ent, removingFlowers: true);
    }

    private void OnFlowersState(Entity<RMCPlantTraitFlowersComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    private void UpdateSprite(EntityUid uid, bool removingFlowers = false)
    {
        if (!TryComp(uid, out RMCPlantComponent? plant) ||
            !TryComp(uid, out RMCPlantGrowthComponent? growth) ||
            !TryComp(uid, out RMCPlantTraitsComponent? traits) ||
            !TryComp(uid, out SpriteComponent? sprite))
        {
            return;
        }

        UpdateSprite(uid, plant, growth, traits, sprite, removingFlowers);
    }

    private void UpdateSprite(
        EntityUid uid,
        RMCPlantComponent plant,
        RMCPlantGrowthComponent growth,
        RMCPlantTraitsComponent traits,
        SpriteComponent sprite,
        bool removingFlowers)
    {
        var state = plant.Dead
            ? "dead"
            : plant.Harvest
                ? "harvest"
                : plant.Age < growth.Maturation
                    ? $"stage-{GetCurrentGrowthStage(plant, growth)}"
                    : $"stage-{growth.GrowthStages}";

        _sprite.LayerMapReserve((uid, sprite), RMCPlantLayers.Plant);
        _sprite.LayerMapReserve((uid, sprite), RMCPlantLayers.Flower);

        _sprite.LayerSetVisible((uid, sprite), RMCPlantLayers.Plant, true);
        _sprite.LayerSetRsi((uid, sprite), RMCPlantLayers.Plant, traits.PlantRsi);
        _sprite.LayerSetRsiState((uid, sprite), RMCPlantLayers.Plant, state);

        var flowers = removingFlowers ? null : CompOrNull<RMCPlantTraitFlowersComponent>(uid);
        var flowerValid = !plant.Dead && flowers != null && !string.IsNullOrEmpty(flowers.Icon);
        _sprite.LayerSetVisible((uid, sprite), RMCPlantLayers.Flower, flowerValid);

        if (flowerValid)
        {
            _sprite.LayerSetRsi((uid, sprite), RMCPlantLayers.Flower, traits.PlantRsi);
            _sprite.LayerSetRsiState((uid, sprite), RMCPlantLayers.Flower, flowers!.Icon!);
            _sprite.LayerSetColor((uid, sprite), RMCPlantLayers.Flower, flowers.Color ?? Color.White);
        }
    }

    private static int GetCurrentGrowthStage(RMCPlantComponent plant, RMCPlantGrowthComponent growth)
    {
        return Math.Clamp((int)(plant.Age * growth.GrowthStages / growth.Maturation) + 1, 1, growth.GrowthStages);
    }
}
