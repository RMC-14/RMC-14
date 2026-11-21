using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardComponent
{
    [DataField]
    public float RestHealFraction = 0.05f;

    [DataField]
    public float AiFeedHealFraction = 0.15f;

    [DataField]
    public float AiFeedRange = 1.5f;

    [DataField]
    public float FoodSearchRange = 6f;

    [DataField]
    public float FoodTargetKeepRange = 5f;

    [ViewVariables]
    public EntityUid? FoodTarget;

    [ViewVariables]
    public bool EatingFood;

    [ViewVariables]
    public int FoodBitesLeft;

    [ViewVariables]
    public TimeSpan NextFoodBiteAt;

    [ViewVariables]
    public TimeSpan NextFoodSearchAt;

    [DataField]
    public int FoodBitesMin = 4;

    [DataField]
    public int FoodBitesMax = 6;

    [DataField]
    public TimeSpan FoodBiteDelayMin = TimeSpan.FromSeconds(1.7);

    [DataField]
    public TimeSpan FoodBiteDelayMax = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan FoodLostCooldown = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan FoodEatenCooldown = TimeSpan.FromSeconds(30);

    [DataField]
    public float FoodTheftRetaliateRange = 2f;

    [DataField]
    public float AiFeedTameRange = 7f;

    [DataField]
    public SoundSpecifier EatingSound = new SoundCollectionSpecifier("eating", AudioParams.Default.WithVolume(-4));

    [DataField]
    public float ForageSpeed = 2.5f;

    [DataField]
    public float DirectFeedHealFraction = 0.10f;

    [DataField]
    public HashSet<string> AllowedTameFactions = new()
    {
        "UNMC",
        "SPP",
        "Halcyon",
        "WeYa",
        "Civilian",
        "CLF",
        "TSE",
        "HEFA",
        "RoyalMarines",
        "Bureau",
    };

    [DataField]
    public HashSet<string> ExcludedTameFactions = new()
    {
        "RMCXeno",
        "RMCDumb",
        "SimpleNeutral",
        "SimpleHostile",
        "Mouse",
        "PetsNT",
    };
}
