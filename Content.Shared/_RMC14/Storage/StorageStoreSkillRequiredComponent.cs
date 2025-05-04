using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStorageSystem))]
public sealed partial class StorageStoreSkillRequiredComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<Entry> Entries = new();

    [DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct Entry(EntityWhitelist Whitelist, SkillWhitelist Skills);
}
