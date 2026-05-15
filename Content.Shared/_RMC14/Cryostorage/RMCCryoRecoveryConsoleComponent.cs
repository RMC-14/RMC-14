using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Cryostorage;

[RegisterComponent]
public sealed partial class RMCCryoRecoveryConsoleComponent : Component
{
    [DataField]
    public bool ExcludeWhitelistedRoles = true;

    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>> ExcludedDepartments = new()
    {
        "CMMilitaryPolice",
    };
}
