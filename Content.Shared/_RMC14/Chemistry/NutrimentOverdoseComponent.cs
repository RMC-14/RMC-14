using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NutrimentOverdoseComponent : Component
{
    [DataField, AutoNetworkedField]
    public float RemainingVolume;

    [DataField]
    public TimeSpan SlowdownDuration = TimeSpan.FromSeconds(1);
}
