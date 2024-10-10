using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.SightRestriction;

[DataDefinition]
public partial struct SightRestrictionDefinition
{
    /// <summary>
    ///     Radius of full sight restriction in tiles counted from screen edge
    /// </summary>
    [DataField]
    public FixedPoint2 ImpairFull = 3.0f;
    /// <summary>
    ///     Radius of partial sight restriction in tiles counted from edge of full sight restriction
    /// </summary>
    [DataField]
    public FixedPoint2 ImpairPartial = 2.0f;

    /// <summary>
    ///     Alpha component of full sight restriction
    /// </summary>
    [DataField]
    public FixedPoint2 AlphaOuter = 1.0f;
    /// <summary>
    ///     Alpha component of unrestricted sight; the alpha of partial sight restriction is a gradient between this and AlphaOuter
    /// </summary>
    [DataField]
    public FixedPoint2 AlphaInner = 0.0f;

    public SightRestrictionDefinition(
        FixedPoint2 impairFull, FixedPoint2 impairPartial,
        FixedPoint2 alphaOuter, FixedPoint2 alphaInner)
    {
        ImpairFull = impairFull;
        ImpairPartial = impairPartial;
        AlphaOuter = alphaOuter;
        AlphaInner = alphaInner;
    }

    // Operator overloads to make comparisons easier
    public static bool operator ==(SightRestrictionDefinition a, SightRestrictionDefinition b)
    {
        return (a.ImpairFull == b.ImpairFull &&
                a.ImpairPartial == b.ImpairPartial &&
                a.AlphaOuter == b.AlphaOuter &&
                a.AlphaInner == b.AlphaInner);
    }

    public static bool operator !=(SightRestrictionDefinition a, SightRestrictionDefinition b)
    {
        return !(a == b);
    }

    // Only accounts for restriction size (radius), not strength (alpha component)!
    public static bool operator <=(SightRestrictionDefinition a, SightRestrictionDefinition b)
    {
        if (a.ImpairFull > b.ImpairFull)
            return false;

        if (a.ImpairPartial > b.ImpairPartial)
            return false;

        return true;
    }

    public static bool operator >=(SightRestrictionDefinition a, SightRestrictionDefinition b)
    {
        if (a.ImpairFull < b.ImpairFull)
            return false;

        if (a.ImpairPartial < b.ImpairPartial)
            return false;

        return true;
    }

    public static bool operator >(SightRestrictionDefinition a, SightRestrictionDefinition b)
    {
        return !(a <= b);
    }

    public static bool operator <(SightRestrictionDefinition a, SightRestrictionDefinition b)
    {
        return !(a >= b);
    }
}
