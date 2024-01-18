using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Entrenching;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(EntrenchingToolSystem))]
public sealed partial class EntrenchingToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DigDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan FillDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public int LayersPerDig = 5;

    [DataField, AutoNetworkedField]
    public int TotalLayers;

    [AutoNetworkedField]
    public EntityCoordinates LastDigLocation;
}

[Serializable, NetSerializable]
public enum EntrenchingToolComponentVisualLayers
{
    Base,
    Folded,
    Dirt
}
