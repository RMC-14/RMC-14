using Content.Shared.Preferences;

namespace Content.Server._RMC14.Preferences;

[RegisterComponent]
[Access(typeof(RMCCharacterSelectionSystem))]
public sealed partial class RMCActiveCharacterComponent : Component
{
    [ViewVariables]
    public HumanoidCharacterProfile? Profile;
}
