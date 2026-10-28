using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Addiction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCAddictedComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<RMCAddictionRecord> Addictions = new();

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1f;
}

[Serializable, DataDefinition]
public sealed partial class RMCAddictionRecord
{
    [DataField(required: true)]
    public string ReagentId = string.Empty;

    [DataField]
    public int Stage = 1;

    [DataField]
    public float AddictionProgression;

    [DataField]
    public float WithdrawalProgression;

    [DataField(required: true)]
    public float Multiplier;
}
