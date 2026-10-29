// ReSharper disable CheckNamespace

using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

public sealed partial class TraitPrototype
{
    [DataField]
    public ProtoId<LanguagePrototype>? Language { get; private set; }
}
