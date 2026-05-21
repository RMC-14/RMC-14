using Robust.Shared.Audio;
using Content.Shared._RMC14.Terrain;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Entrenching;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(BarricadeSystem))]
public sealed partial class EntrenchingToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DigDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan FillDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public int LayersPerDig = 5;

    [DataField, AutoNetworkedField]
    public int TotalLayers;

    [DataField, AutoNetworkedField]
    public RMCTerrainMaterial DigMaterial = RMCTerrainMaterial.None;

    [AutoNetworkedField]
    public EntityCoordinates LastDigLocation;

    [DataField, AutoNetworkedField]
    public SoundSpecifier DigSound = new SoundCollectionSpecifier("CMEntrenchingThud", AudioParams.Default.WithVolume(-3));

    [DataField, AutoNetworkedField]
    public SoundSpecifier FillSound = new SoundCollectionSpecifier("CMEntrenchingRustle", AudioParams.Default.WithVolume(-6));
}

[Serializable, NetSerializable]
public enum EntrenchingToolComponentVisualLayers
{
    Base,
    Folded,
    Dirt
}
