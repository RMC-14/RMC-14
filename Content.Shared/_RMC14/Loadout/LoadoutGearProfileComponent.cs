using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Loadout;

[RegisterComponent]
public sealed partial class LoadoutGearProfileComponent : Component
{
    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> StartingGear;

    [DataField(required: true)]
    public List<string> ManagedSlots = [];

    [DataField]
    public List<string> PreserveSlots = [];

    public bool Applied;
}
