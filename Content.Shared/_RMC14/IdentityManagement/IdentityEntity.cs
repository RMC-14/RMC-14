namespace Content.Shared._RMC14.IdentityManagement;

public readonly record struct IdentityEntity(EntityUid Entity, string Name) : ILocValue
{
    public static implicit operator EntityUid(IdentityEntity ent)
    {
        return ent.Entity;
    }

    public static implicit operator string(IdentityEntity ent)
    {
        return ent.Name;
    }

    public string Format(LocContext ctx)
    {
        return Name;
    }

    public object Value => Entity;

    public override string ToString()
    {
        return Name;
    }
}
