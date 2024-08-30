using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.HUD;

/// <param name="HolocardIcon">The prototype id of the icon shown on a entity with the medHUD.</param>
/// <param name="Description">The description shown on a health scan.</param>
public record struct HolocardData(ProtoId<HealthIconPrototype>? HolocardIcon, LocId Description);
