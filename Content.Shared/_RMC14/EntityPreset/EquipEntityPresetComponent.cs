using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.EntityPreset;

[RegisterComponent]
[Access(typeof(EntityPresetSystem))]
public sealed partial class EquipEntityPresetComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<EntityPresetComponent> Preset;
}
