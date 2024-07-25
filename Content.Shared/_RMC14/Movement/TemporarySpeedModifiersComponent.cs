using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TemporarySpeedModifiersSystem))]
public sealed partial class TemporarySpeedModifiersComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<(TimeSpan ExpiresAt, float Walk, float Sprint)> Modifiers = new();
}

[DataRecord, Serializable, NetSerializable]
public record struct TemporarySpeedModifierSet(
    TimeSpan Duration,
    float Walk,
    float Sprint
);
