using Content.Shared._RMC14.ERT;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.ERT;

internal sealed record RMCERTCall(EntityPrototype Prototype, RMCERTCallComponent Comp)
{
    public EntProtoId<RMCERTCallComponent> Id => new(Prototype.ID);
    public string Name => Prototype.Name;
    public string Organization => Comp.Organization is { } organization ? Loc.GetString(organization) : string.Empty;
    public string Category => Loc.GetString(Comp.Category);
    public string? AdminButtonLabel => Comp.AdminButtonLabel is { } label ? Loc.GetString(label) : null;
}
