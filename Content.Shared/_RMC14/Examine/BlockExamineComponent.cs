using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Examine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMExamineSystem))]
public sealed partial class BlockExamineComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
