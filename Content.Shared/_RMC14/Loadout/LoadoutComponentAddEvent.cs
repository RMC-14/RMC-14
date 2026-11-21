using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Loadout;

[ByRefEvent]
public record struct LoadoutComponentAddEvent(EntityUid Entity, LoadoutPrototype Loadout);
