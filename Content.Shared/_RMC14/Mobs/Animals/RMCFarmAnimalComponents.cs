using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCCowTippableComponent : Component
{
    [DataField]
    public TimeSpan TipTime = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan TipCooldown = TimeSpan.FromSeconds(10);

    [ViewVariables]
    public TimeSpan NextTipAt;

    [ViewVariables]
    public TimeSpan TippedUntil;
}

[RegisterComponent]
public sealed partial class RMCGoatTemperComponent : Component
{
    [DataField]
    public float SearchRange = 6f;

    [DataField]
    public float MadChance = 0.01f;

    [DataField]
    public float CalmChance = 0.10f;

    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextThinkAt;
}

[RegisterComponent]
public sealed partial class RMCChickenComponent : Component
{
}

[RegisterComponent]
public sealed partial class RMCFarmAnimalEmoteComponent : Component
{
    [DataField]
    public List<string> Emotes = new();

    [DataField]
    public float EmoteChance = 0.45f;

    [DataField]
    public TimeSpan EmoteCooldownMin = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan EmoteCooldownMax = TimeSpan.FromSeconds(50);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEmoteAt;
}

[RegisterComponent]
public sealed partial class RMCChickenFedEggLayerComponent : Component
{
    [DataField]
    public EntProtoId EggPrototype = "FoodEgg";

    [DataField]
    public EntProtoId FertilizedEggPrototype = "RMCFoodEggChickenFertilized";

    [DataField]
    public float FertilizedEggChance = 0.10f;

    [DataField]
    public int MaxNearbyChickens = 50;

    [DataField]
    public float ChickenCapRange = 64f;

    [DataField]
    public int MaxEggCredits = 8;

    [DataField]
    public int MinFeedCredits = 1;

    [DataField]
    public int MaxFeedCredits = 4;

    [DataField]
    public TimeSpan LayCheckCooldown = TimeSpan.FromSeconds(15);

    [DataField]
    public float LayChance = 0.45f;

    [DataField]
    public string FeedTag = "Wheat";

    [ViewVariables]
    public int EggCredits;

    [ViewVariables]
    public TimeSpan NextLayCheckAt;
}

[RegisterComponent]
public sealed partial class RMCChickenEggHatchComponent : Component
{
    [DataField]
    public EntProtoId SpawnPrototype = "RMCMobChick";

    [DataField]
    public TimeSpan HatchMin = TimeSpan.FromSeconds(90);

    [DataField]
    public TimeSpan HatchMax = TimeSpan.FromSeconds(180);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan HatchAt;
}

[RegisterComponent]
public sealed partial class RMCChickGrowthComponent : Component
{
    [DataField]
    public List<EntProtoId> MaturePrototypes = new()
    {
        "RMCMobChicken",
        "RMCMobChicken1",
        "RMCMobChicken2",
    };

    [DataField]
    public TimeSpan GrowMin = TimeSpan.FromSeconds(90);

    [DataField]
    public TimeSpan GrowMax = TimeSpan.FromSeconds(180);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan GrowAt;
}
