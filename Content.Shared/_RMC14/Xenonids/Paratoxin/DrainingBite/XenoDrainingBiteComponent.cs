using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Paratoxin.DrainingBite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDrainingBiteComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan MinStunTime = TimeSpan.FromSeconds(0.2);

    [DataField, AutoNetworkedField]
    public float ChemicalDrainStackMultiplier = 1.0f / 3.0f;

    [DataField, AutoNetworkedField]
    public string DrainGroup = "Medicine";

    [DataField, AutoNetworkedField]
    public float ProportialStacksToRemoveMultiplier = 1.0f / 3.0f;

    [DataField, AutoNetworkedField]
    public float StackDivisor = 10;

    [DataField, AutoNetworkedField]
    public EntProtoId BiteEffect = "RMCEffectHeadbite";

    [DataField, AutoNetworkedField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_bite2.ogg");
}
