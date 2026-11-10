using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Plates;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCArmorPlateSystem))]
public sealed partial class RMCEmergencyInjectorPlateComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Cocktail = new()
    {
        ["RMCOxycodone"] = 20,
        ["CMBicaridine"] = 30,
        ["CMKelotane"] = 30,
        ["CMMeralyne"] = 15,
        ["CMDermaline"] = 15,
        ["CMDexalinPlus"] = 1,
        ["CMInaprovaline"] = 30,
    };

    [DataField, AutoNetworkedField]
    public bool Used;

    [DataField, AutoNetworkedField]
    public RMCEmergencyInjectorOverdose OverdoseProtection = RMCEmergencyInjectorOverdose.Dynamic;

    [DataField]
    public int RecyclableValue = 100;

    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/_RMC14/Medical/air_release.ogg");
}

[Serializable, Robust.Shared.Serialization.NetSerializable]
public enum RMCEmergencyInjectorOverdose : byte
{
    Off,
    Strict,
    Dynamic,
}
