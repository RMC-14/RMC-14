using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Evacuation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedEvacuationSystem))]
public sealed partial class LifeboatComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public float EarlyCrashChance = 0.75f;
}
