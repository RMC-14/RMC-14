using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStorageSystem))]
public sealed partial class StorageNestedOpenSkillRequiredComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills = new();
}
