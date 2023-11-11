using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Admin;

[Serializable, NetSerializable]
public sealed class CMAdminVerb : Verb
{
    public override int TypePriority => int.MaxValue - 1;
}
