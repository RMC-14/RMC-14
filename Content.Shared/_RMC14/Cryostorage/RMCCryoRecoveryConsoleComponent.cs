using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Cryostorage;

/// <summary>
/// Configures an RMC requisitions console that recovers equipment from vanilla cryostorage.
/// The console does not own stored bodies; it reads <see cref="Content.Shared.Bed.Cryostorage.CryostorageComponent.StoredPlayers"/>
/// and validates every recovery action on the server.
/// </summary>
[RegisterComponent]
public sealed partial class RMCCryoRecoveryConsoleComponent : Component
{
    /// <summary>
    /// Prevents equipment recovery from roles marked as whitelisted in their <see cref="JobPrototype"/>.
    /// This is separate from department filtering so maps can opt out without editing code.
    /// </summary>
    [DataField]
    public bool ExcludeWhitelistedRoles = true;

    /// <summary>
    /// Departments hidden from this console. MP stays unavailable here while the vanilla chamber UI is left intact.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>> ExcludedDepartments = new()
    {
        "CMMilitaryPolice",
    };
}
