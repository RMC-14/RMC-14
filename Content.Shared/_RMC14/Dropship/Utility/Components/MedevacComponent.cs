namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent]
public sealed partial class MedevacComponent : Component
{
    public const string AnimationState = "medevac_system_active";
    public const string AnimationDelay = "medevac_system_delay";

    public bool IsActivated = false;
    [DataField]
    public TimeSpan DelayLength = TimeSpan.FromSeconds(3);
}
