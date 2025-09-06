using Content.Shared._RMC14.Light.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Shared._RMC14.Light.EntitySystems;

/// <summary>
/// System that automatically activates lights on entities with RMCLightStartsOnInitComponent during initialization.
/// </summary>
public sealed class RMCLightStartsOnInitSystem : EntitySystem
{
    [Dependency] private readonly ItemTogglePointLightSystem _itemTogglePointLight = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCLightStartsOnInitComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RMCLightStartsOnInitComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<ItemTogglePointLightComponent>(ent.Owner, out var lightToggle))
        {
            var toggleEvent = new ItemToggledEvent(Predicted: false, Activated: true, User: null);
            RaiseLocalEvent(ent.Owner, ref toggleEvent);
        }
    }
}
