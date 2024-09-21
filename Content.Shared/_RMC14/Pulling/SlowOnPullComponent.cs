using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Pulling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCPullingSystem))]
public sealed partial class SlowOnPullComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Multiplier = 1;

    [DataField, AutoNetworkedField]
    public List<SlowdownWhitelist> Slowdowns = new();

    [DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct SlowdownWhitelist(float Multiplier, EntityWhitelist Whitelist);
}
