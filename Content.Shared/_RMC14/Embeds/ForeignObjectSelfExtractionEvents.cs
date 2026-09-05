using Content.Shared.Alert;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Embeds;

public sealed partial class ForeignObjectSelfExtractionAlertEvent : BaseAlertEvent;

[Serializable, NetSerializable]
public sealed partial class ForeignObjectSelfExtractionDoAfterEvent : SimpleDoAfterEvent
{
    public readonly BodyPartType BodyPart;
    public readonly BodyPartSymmetry Symmetry;

    public ForeignObjectSelfExtractionDoAfterEvent(BodyPartType bodyPart, BodyPartSymmetry symmetry)
    {
        BodyPart = bodyPart;
        Symmetry = symmetry;
    }
}