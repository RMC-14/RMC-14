using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Humanoid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCHumanoidAppearanceSystem))]
public sealed partial class HiddenAppearanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCHumanoidAppearance? Appearance;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
