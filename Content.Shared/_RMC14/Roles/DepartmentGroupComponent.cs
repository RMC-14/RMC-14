using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class DepartmentGroupComponent : Component
{
    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public Color Color;

    [DataField(required: true)]
    public HashSet<ProtoId<DepartmentPrototype>> Departments = new();
}
