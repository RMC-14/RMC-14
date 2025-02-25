using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCClothingSystem))]
public sealed partial class RMCClothingFoldableComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? ActivatedPrefix;

    [DataField, AutoNetworkedField]
    public List<FoldableType> Types = new();
}

/// <summary>
/// Prefix is the clothing prefix when this foldable type is activated. BlacklistedPrefix will prevent this foldable type
/// from activating while the blacklisted one is activated.
/// </summary>
[DataRecord]
[Serializable, NetSerializable]
public readonly record struct FoldableType(string Prefix, LocId Name, int Priority, string? BlacklistedPrefix, LocId? BlacklistPopup);
