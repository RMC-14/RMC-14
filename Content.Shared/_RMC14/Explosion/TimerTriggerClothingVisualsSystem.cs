using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared._RMC14.Explosion;

/// <summary>
/// Updates clothing equipped visuals when timer trigger is activated.
/// </summary>
public sealed class TimerTriggerClothingVisualsSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimerTriggerClothingVisualsComponent, RMCActiveTimerTriggerEvent>(OnTimerActivated);
    }

    private void OnTimerActivated(Entity<TimerTriggerClothingVisualsComponent> ent, ref RMCActiveTimerTriggerEvent args)
    {
        _clothing.SetEquippedPrefix(ent, ent.Comp.PrimedPrefix);
    }
}
