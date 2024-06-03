using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Marines.Skills;

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct Skills(
    int Antagonist,
    int Construction,
    int Cqc,
    int Domestics,
    int Endurance,
    int Engineer,
    int Execution,
    int Firearms,
    int Fireman,
    int Intel,
    int Jtac,
    int Leadership,
    int Medical,
    int MeleeWeapons,
    int Navigations,
    int Overwatch,
    int Pilot,
    int Police,
    // forklift certified
    int PowerLoader,
    int Research,
    int Smartgun,
    int SpecialistWeapons,
    // no longer a week away
    int Surgery,
    int Vehicles
);
