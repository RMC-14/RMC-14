using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMedevacSystem))]
public sealed partial class MedevacComponent : Component
{
    public const string AnimationState = "medevac_system_active";
    public const string AnimationDelay = "medevac_system_delay";

    [DataField, AutoNetworkedField]
    public bool IsActivated;

    [DataField, AutoNetworkedField]
    public TimeSpan DelayLength = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public EntityUid? Target;
}
