namespace Content.Server._RMC14.Scorch;

[RegisterComponent]
public sealed partial class RMCScorchEffectOnSpawnComponent : Component
{
    /// <summary>
    /// Probability that a decal will be created.
    /// </summary>
    [DataField]
    public float? Probability = 1.0f;

    /// <summary>
    /// Maximum decals to spawn on a tile.
    /// </summary>
    [DataField]
    public int? TileLimit = 1;

    /// <summary>
    /// Enable scatter on decals spawned, from the center of entity.
    /// </summary>
    [DataField]
    public bool Scatter = false;

    /// <summary>
    /// Enable random decal rotation.
    /// </summary>
    [DataField]
    public bool RandomRotation = false;

    /// <summary>
    /// Tags used for pool of decals.
    /// </summary>
    [DataField]
    public string DecalTag = "RMCScorchSmall";

}
