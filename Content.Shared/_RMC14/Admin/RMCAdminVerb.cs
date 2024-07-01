using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Admin;

[Serializable, NetSerializable]
public sealed class RMCAdminVerb : Verb
{
    public override int TypePriority => int.MaxValue - 1;
}
