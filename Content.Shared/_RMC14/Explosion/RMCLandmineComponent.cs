using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCLandmineComponent : Component
{
    /// <summary>
    ///     Whether the claymore is ready to explode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Armed;

    /// <summary>
    ///     How long it takes to place the mine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PlacementDelay = 4;

    /// <summary>
    ///     How long it takes to disarm the mine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DisarmDelay = 3;

    /// <summary>
    ///     The tool quality required to disarm the mine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> DisarmTool = "Pulsing";

    /// <summary>
    ///     The faction the claymore will ignore.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent>? Faction;

    /// <summary>
    ///     The amount of times the claymore has been shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ShotStacks;

    /// <summary>
    ///     The amount of times the claymore has to be shot for it to explode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ShotStackLimit = 2;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeploySound;
}
