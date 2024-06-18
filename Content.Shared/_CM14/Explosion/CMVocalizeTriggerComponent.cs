using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMExplosionSystem))]
public sealed partial class CMVocalizeTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId UserPopup = "cm-grenade-primed-user";

    [DataField, AutoNetworkedField]
    public LocId OthersPopup = "cm-grenade-primed-others";

    [DataField, AutoNetworkedField]
    public PopupType PopupType = PopupType.MediumCaution;

    // TODO CM14 sounds for other species
    [DataField, AutoNetworkedField]
    public Dictionary<Sex, SoundSpecifier> Sounds = new()
    {
        [Sex.Female] = new SoundCollectionSpecifier("CMHumanFemaleGrenadeThrow"),
        [Sex.Male] = new SoundCollectionSpecifier("CMHumanMaleGrenadeThrow"),
        [Sex.Unsexed] = new SoundCollectionSpecifier("CMHumanMaleGrenadeThrow")
    };
}
