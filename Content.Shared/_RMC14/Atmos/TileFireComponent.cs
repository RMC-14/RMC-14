using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class TileFireComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<TileFireComponent>? Id;

    [DataField, AutoNetworkedField]
    public bool ExtinguishInstantly = true;

    [DataField, AutoNetworkedField]
    public float PatExtinguishMultiplier = 1;

    [DataField, AutoNetworkedField]
    public float SprayExtinguishMultiplier = 1;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan SpawnedAt;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromMinutes(1);

    [DataField, AutoNetworkedField]
    public TimeSpan BigFireDuration = TimeSpan.FromSeconds(0.5);
}

[Serializable, NetSerializable]
public enum TileFireLayers
{
    Base,
}

[Serializable, NetSerializable]
public enum TileFireVisuals
{
    One,
    Two,
    Three,
    Four,
}
