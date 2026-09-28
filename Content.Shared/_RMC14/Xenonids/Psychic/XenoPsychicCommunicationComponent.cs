using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Psychic;

[RegisterComponent]
public sealed partial class XenoPsychicCommunicationComponent : Component
{
    [DataField]
    public float WhisperRange = 7;

    [DataField]
    public float RadianceRange = 12;

    [DataField]
    public FixedPoint2 RadiancePlasmaCost = 100;

    [DataField]
    public FixedPoint2 GiveOrderPlasmaCost = 100;

    [DataField]
    public int CharacterLimit = 200;
}
