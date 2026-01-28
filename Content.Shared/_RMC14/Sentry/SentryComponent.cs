using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Sentry.Laptop;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sentry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SentrySystem), typeof(SharedSentryLaptopSystem))]
public sealed partial class SentryComponent : Component
{
    [DataField, AutoNetworkedField]
    public SentryMode Mode;

    [DataField, AutoNetworkedField]
    public TimeSpan DeployDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan UndeployDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan MagazineDelay = TimeSpan.FromSeconds(7);

    [DataField, AutoNetworkedField]
    public int DefenseCheckRange = 2;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ScrewdriverSound = new SoundPathSpecifier("/Audio/Items/screwdriver.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillEngineer";

    [DataField, AutoNetworkedField]
    public int SkillLevel = 2;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? MagazineSwapSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/unload.ogg");

    [DataField, AutoNetworkedField]
    public string? DeployFixture = "sentry";

    [DataField, AutoNetworkedField]
    public Angle MaxDeviation = Angle.FromDegrees(75);

    [DataField, AutoNetworkedField]
    public EntProtoId<BallisticAmmoProviderComponent>? StartingMagazine = "RMCMagazineSentry";

    [DataField, AutoNetworkedField]
    public string ContainerSlotId = "gun_magazine";

    [DataField, AutoNetworkedField]
    public EntProtoId[]? Upgrades = ["RMCSentrySniper", "RMCSentryShotgun", "RMCSentryMini", "RMCSentryOmni"];

    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype>? MagazineTag = "RMCMagazineSentry";

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> DelaySkill = "RMCSkillConstruction";

    [DataField, AutoNetworkedField]
    public EntityUid? Camera;

    [DataField, AutoNetworkedField]
    public float LowAmmoThreshold = 0.25f;

    [DataField, AutoNetworkedField]
    public float CriticalHealthThreshold = 0.25f;

    [DataField, AutoNetworkedField]
    public TimeSpan LastLowAmmoAlert;

    [DataField, AutoNetworkedField]
    public TimeSpan LastHealthAlert;

    [DataField, AutoNetworkedField]
    public TimeSpan LastTargetAlert;

    [DataField, AutoNetworkedField]
    public TimeSpan AlertCooldown = TimeSpan.FromSeconds(5);
}

[Serializable, NetSerializable]
public enum SentryMode
{
    Item,
    Off,
    On,
}

[Serializable, NetSerializable]
public enum SentryLayers
{
    Layer,
}
