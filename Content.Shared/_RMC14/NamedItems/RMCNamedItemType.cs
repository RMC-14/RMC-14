using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.NamedItems;

[Serializable, NetSerializable]
public enum RMCNamedItemType
{
    PrimaryGun,
    Sidearm,
    Helmet,
    Armor,
    Sentry,
}
