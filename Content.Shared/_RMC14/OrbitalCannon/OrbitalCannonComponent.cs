using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonComponent : Component
{
    [DataField, AutoNetworkedField]
    public string CannonChamberContainer = "rmc_orbital_cannon_chamber";

    [DataField, AutoNetworkedField]
    public string CannonRsiPath = "_RMC14/Structures/orbital_cannon.rsi";

    [DataField, AutoNetworkedField]
    public string BaseState = "obc";

    [DataField, AutoNetworkedField]
    public string BaseLayerKey = "CannonLayerKey";

    [DataField, AutoNetworkedField]
    public EntProtoId<OrbitalCannonWarheadComponent>[] WarheadTypes =
        ["RMCOrbitalCannonWarheadExplosive", "RMCOrbitalCannonWarheadIncendiary", "RMCOrbitalCannonWarheadCluster", "RMCOrbitalCannonWarheadAegis"];

    [DataField, AutoNetworkedField]
    public int[] PossibleFuelRequirements = [4, 5, 6, 6];

    [DataField, AutoNetworkedField]
    public List<WarheadFuelRequirement> FuelRequirements = new();

    [DataField, AutoNetworkedField]
    public OrbitalCannonStatus Status = OrbitalCannonStatus.Unloaded;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastToggledAt;

    [DataField, AutoNetworkedField]
    public TimeSpan ToggleCooldown = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LoadItemSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_1.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? UnloadItemSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_2.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LoadSound = new SoundPathSpecifier("/Audio/_RMC14/Mecha/powerloader_buckle.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? UnloadSound = new SoundPathSpecifier("/Audio/_RMC14/Mecha/powerloader_unbuckle.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ChamberSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_2.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FireSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Vehicles/smokelauncher_fire.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? GroundAlertSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/ob_alert.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TravelSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/gun_orbital_travel.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? AegisBoomSound = new SoundPathSpecifier("/Audio/_RMC14/Explosion/aegis-close.ogg");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastFireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan FireCooldown = TimeSpan.FromSeconds(500);

    [DataField]
    public EntProtoId? TrayPrototype = "RMCOrbitalCannonTray";

    [DataField]
    public Vector2 TraySpawnOffset = new(0.78f, 0.69f);

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedTray;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? UnloadingTrayAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UnloadingTrayDelay = TimeSpan.FromSeconds(1);
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct WarheadFuelRequirement(EntProtoId<OrbitalCannonWarheadComponent> Warhead, int Fuel);

[Serializable, NetSerializable]
public enum OrbitalCannonStatus
{
    Unloaded = 0,
    Loaded,
    Chambered,
}
