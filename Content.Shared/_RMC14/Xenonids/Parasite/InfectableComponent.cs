using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InfectableComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool BeingInfected = false;

    [DataField, AutoNetworkedField]
    public Dictionary<Sex, SoundSpecifier> Sound = new()
    {
        [Sex.Male] = new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/infected_male.ogg"),
        [Sex.Female] = new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/infected_female.ogg"),
        [Sex.Unsexed] = new SoundPathSpecifier("/Audio/_RMC14/Voice/Human/infected_male.ogg")
    };

    /* TODO add this
    [DataField, AutoNetworkedField]
    public Dictionary<Sex, SoundSpecifier> PreburstSound = new()
    {
        [Sex.Male] = new SoundCollectionSpecifier("RMCMalePreburstScreams")
        {
            Params = AudioParams.Default.WithVolume(-3f),
        },

        [Sex.Female] = new SoundCollectionSpecifier("RMCFemalePreburstScreams")
        {
            Params = AudioParams.Default.WithVolume(-3f),
        }
    };
    */
}
