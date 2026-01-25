using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Stamina;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCStaminaComponent : Component
{
    [DataField, AutoNetworkedField]
    public double Current = 100;

    [DataField]
    public double Max = 100;

    [DataField]
    public int RegenPerTick = 6;

    [DataField, AutoNetworkedField]
    public int Level = 0;

    [DataField]
    public TimeSpan TimeBetweenChecks = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan NextRegen;

    [DataField, AutoNetworkedField]
    public TimeSpan NextCheck;

    [DataField]
    public ProtoId<AlertPrototype> StaminaAlert = "RMCStamina";

    [DataField]
    public TimeSpan RestPeriod = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public int[] TierThresholds = [100, 70, 30, 10, 0];

    [DataField, AutoNetworkedField]
    public float[] SpeedModifiers = [0, 1.5f, 2.75f, 3.75f, 4.5f];

    [DataField, AutoNetworkedField]
    public TimeSpan EffectTime = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan DazeTime = TimeSpan.FromSeconds(6);
}
