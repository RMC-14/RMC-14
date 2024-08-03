namespace Content.Shared._RMC14.Areas;

[Flags]
public enum AreaFlags
{
    None = 0,
    CAS = 1 << 0,
    Fulton = 1 << 1,
    Lasing = 1 << 2,
    Mortar = 1 << 3,
    Medevac = 1 << 4,
    OB = 1 << 5,
    SupplyDrop = 1 << 6,
    AvoidBioscan = 1 << 7,
    NoTunnel = 1 << 8,
    Unweedable = 1 << 9,
}
