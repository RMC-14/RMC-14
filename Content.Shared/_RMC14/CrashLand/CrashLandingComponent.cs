using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.CrashLand;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrashLandingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float RemainingTime = 1f;

    [DataField, AutoNetworkedField]
    public bool DoDamage;
}
