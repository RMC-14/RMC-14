using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Stun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSizeStunSystem))]
public sealed partial class RMCStunOnTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 7;

    [DataField, AutoNetworkedField]
    public TimeSpan Stun = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public TimeSpan Flash = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan FlashAdditionalStunTime = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public float Probability = 1;

    [DataField, AutoNetworkedField]
    public List<RMCStunOnTriggerFilter>? Filters;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RMCStunOnTriggerFilter
{
    [DataField]
    public float? Range;

    [DataField]
    public TimeSpan? Stun;

    [DataField]
    public TimeSpan? Flash;

    [DataField]
    public TimeSpan? FlashAdditionalStunTime;

    [DataField]
    public float? Probability;

    [DataField]
    public EntityWhitelist Whitelist;
}
