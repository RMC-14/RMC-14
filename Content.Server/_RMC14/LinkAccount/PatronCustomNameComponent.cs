using Robust.Shared.Network;

namespace Content.Server._RMC14.LinkAccount;

[RegisterComponent]
[Access(typeof(LinkAccountSystem))]
public sealed partial class PatronCustomNameComponent : Component
{
    [DataField(required: true)]
    public NetUserId User;

    [DataField]
    public string? Tier;

    [DataField]
    public string? Name;

    [DataField]
    public string? Description;
}
