using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlammableSystem))]
public  partial class DirectionalTileFireOnTriggerComponent : Component
{
    /// <summary>
    ///     How long the line of fire should be
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Range = 2;

    /// <summary>
    ///     The range when spreading the fire diagonally
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DiagonalRange = 1;

    /// <summary>
    ///     The amount of fires spawned on each side of the initial target
    /// </summary>
    [DataField]
    public int Width = 1;

    /// <summary>
    ///     If the initial hit tile should spawn fires to it's sides
    /// </summary>
    [DataField]
    public bool InitialSpread;

    /// <summary>
    ///     The direction the entity is facing
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction Direction = Direction.South;

    [DataField, AutoNetworkedField]
    public bool Rebounded;

    /// <summary>
    ///     The spawned fire prototype
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCTileFire";

    /// <summary>
    ///     The sound made upon spawning the fire
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_RMC14/Effects/hit_on_shattered_glass.ogg");

    /// <summary>
    ///     The intensity of the fire
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Intensity;

    /// <summary>
    ///     The duration of the fire
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Duration;
}
