using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Skills.Pamphlets;

[ByRefEvent]
public readonly record struct SkillPamphletGrantLanguageEvent(
    EntityUid User,
    ProtoId<LanguagePrototype> Language);
