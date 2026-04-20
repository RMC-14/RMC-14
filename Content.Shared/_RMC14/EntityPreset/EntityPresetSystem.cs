using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Inventory;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EntityPreset;

public sealed class EntityPresetSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RMCRandomizedPresetSystem _preset = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EquipEntityPresetComponent, MapInitEvent>(OnMapInit,
            after: [typeof(SharedCMInventorySystem), typeof(RMCHandsSystem), typeof(CMArmorSystem)]);
    }

    private void OnMapInit(Entity<EquipEntityPresetComponent> ent, ref MapInitEvent args)
    {
        TryApplyPreset(ent);
    }

    private void TryApplyPreset(Entity<EquipEntityPresetComponent> ent)
    {
        if (_net.IsClient)
            return;

        RemCompDeferred<EquipEntityPresetComponent>(ent);
        ApplyPreset(ent, ent.Comp.Preset);
    }

    public void ApplyPreset(EntityUid entity, EntProtoId<EntityPresetComponent> preset)
    {
        if (!preset.TryGet(out var comp, _prototypes, _compFactory))
            return;

        _preset.ApplyPreset(entity, comp);
    }
}
