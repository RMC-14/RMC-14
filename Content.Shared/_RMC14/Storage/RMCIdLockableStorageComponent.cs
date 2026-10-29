using System.Collections.Generic;
using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Storage;

/// <summary>
/// Adds a simple ID lock to storage that binds ownership to the printed name on an ID card.
/// Optional override access is configured entirely in the prototype.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCIdLockableStorageSystem))]
public sealed partial class RMCIdLockableStorageComponent : Component
{
    /// <summary>
    /// Current lock state shown to gameplay and visuals.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked;

    /// <summary>
    /// Trimmed ID card full name currently bound to this storage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? OwnerName;

    /// <summary>
    /// Access tags that can unlock this storage in addition to the owner.
    /// An empty list means the storage has no override path.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> OverrideAccesses = new();
}
