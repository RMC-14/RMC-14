// ReSharper disable StructCanBeMadeReadOnly

using Robust.Shared.Map.Enumerators;

namespace Content.Shared._RMC14.Map;

public struct RMCAnchoredEntitiesEnumerator(
    SharedTransformSystem transform,
    AnchoredEntitiesEnumerator enumerator,
    DirectionFlag facing = DirectionFlag.None
) : IDisposable
{
    // ReSharper disable once CollectionNeverUpdated.Local
    public static readonly RMCAnchoredEntitiesEnumerator Empty = new(default!, AnchoredEntitiesEnumerator.Empty);

    public bool MoveNext(out EntityUid uid)
    {
        while (enumerator.MoveNext(out var uidNullable))
        {
            if (facing == DirectionFlag.None)
            {
                uid = uidNullable.Value;
                return true;
            }

            if ((transform.GetWorldRotation(uidNullable.Value).GetDir().AsFlag() & facing) == 0)
                continue;

            uid = uidNullable.Value;
            return true;
        }

        uid = default;
        return false;
    }

    public void Dispose()
    {
        enumerator.Dispose();
    }
}
