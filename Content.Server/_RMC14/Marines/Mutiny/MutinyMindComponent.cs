using Content.Shared._RMC14.Marines.Mutiny;

namespace Content.Server._RMC14.Marines.Mutiny;

/// <summary>
///     Canonical mutiny state stored on a mind and projected onto its current body.
/// </summary>
[RegisterComponent, Access(typeof(MutinyRuleSystem))]
public sealed partial class MutinyMindComponent : Component
{
    [DataField]
    public EntityUid Rule;

    [DataField]
    public bool IsLeader;

    [DataField]
    public bool AcceptedRecruit;

    [DataField]
    public MutinySide? Side;
}
