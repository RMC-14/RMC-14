using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Prototypes;

public static class CMPrototypeExtensions
{
    public static IEnumerable<T> EnumerateCM<T>(this IPrototypeManager prototypes) where T : class, IPrototype, ICMSpecific
    {
        return prototypes.EnumeratePrototypes<T>().Where(p => p.IsCM);
    }

    public static bool TryCM<T>(this IPrototypeManager prototypes, string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype, ICMSpecific
    {
        prototype = default;

        if (!prototypes.TryIndex(id, out T? proto) ||
            !proto.IsCM)
        {
            return false;
        }

        prototype = proto;
        return true;
    }
}
