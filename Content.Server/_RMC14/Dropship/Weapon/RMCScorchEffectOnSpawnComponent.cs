namespace Content.Server._RMC14.Dropship.Weapon;

[RegisterComponent]
public sealed partial class RMCScorchEffectOnSpawnComponent : Component
{

    [DataField("probability")]
    public float? Probability = 1.0f;

    [DataField("tileLimit")]
    public int? TileLimit = 0;

}
