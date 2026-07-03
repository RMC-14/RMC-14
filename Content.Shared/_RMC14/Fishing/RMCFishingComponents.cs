using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Fishing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFishingRodComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Deployed;

    [DataField, AutoNetworkedField]
    public RMCFishingRodState State = RMCFishingRodState.Idle;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentFisher;

    [DataField, AutoNetworkedField]
    public Direction Direction = Direction.South;

    [DataField]
    public string BaitSlotId = "bait";

    [DataField]
    public TimeSpan DeployDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan PackDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan WaitMin = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan WaitMax = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan BiteMin = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan BiteMax = TimeSpan.FromSeconds(2);

    [DataField]
    public int CommonWeight = 80;

    [DataField]
    public int UncommonWeight = 40;

    [DataField]
    public int RareWeight = 5;

    [DataField]
    public int UltraRareWeight = 1;

    [DataField]
    public ProtoId<RMCFishingLootPrototype> Loot = "RMCFishingLootGeneric";

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/_RMC14/Fishing/fishing_Line.ogg");

    [DataField]
    public SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/_RMC14/Fishing/bobber_water_splash.ogg");

    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/_RMC14/Fishing/fishing_fail_splash.ogg");

    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/_RMC14/Fishing/fishing_set_hook.ogg");

    [DataField]
    public int WaitToken;

    [DataField, AutoNetworkedField]
    public int BiteToken;

    [DataField]
    public TimeSpan BiteEndsAt;
}

[RegisterComponent]
public sealed partial class RMCFishBaitComponent : Component
{
    [DataField]
    public int CommonModifier = -10;

    [DataField]
    public int UncommonModifier = 20;

    [DataField]
    public int RareModifier;

    [DataField]
    public int UltraRareModifier;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFishComponent : Component
{
    [DataField]
    public int MinLength = 1;

    [DataField]
    public int MaxLength = 5;

    [DataField, AutoNetworkedField]
    public int Length;

    [DataField]
    public float MinScale = 0.5f;

    [DataField]
    public float MaxScale = 1.5f;

    [DataField]
    public bool Guttable = true;

    [DataField, AutoNetworkedField]
    public bool Gutted;

    [DataField]
    public List<EntitySpawnEntry> BaseGutSpawns = new()
    {
        new EntitySpawnEntry { PrototypeId = "RMCFoodMeatFish" },
    };

    [DataField]
    public List<EntitySpawnEntry> ExtraGutSpawns = new()
    {
        new EntitySpawnEntry { PrototypeId = "RMCFoodMeatFish" },
    };
}

[RegisterComponent]
public sealed partial class RMCFishingSpearComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField]
    public float FailChance = 0.6f;

    [DataField]
    public int CommonWeight = 60;

    [DataField]
    public int UncommonWeight = 15;

    [DataField]
    public int RareWeight = 5;

    [DataField]
    public int UltraRareWeight = 1;

    [DataField]
    public ProtoId<RMCFishingLootPrototype> Loot = "RMCFishingLootGeneric";

    [DataField]
    public bool Busy;
}

[Serializable, NetSerializable]
public enum RMCFishingRodState : byte
{
    Idle,
    Waiting,
    Biting,
}

[Serializable, NetSerializable]
public enum RMCFishingRodVisuals : byte
{
    Deployed,
    State,
}

[Serializable, NetSerializable]
public enum RMCFishVisuals : byte
{
    Gutted,
}
