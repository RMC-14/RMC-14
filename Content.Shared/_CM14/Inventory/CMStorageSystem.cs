using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared._CM14.Inventory;

public sealed class CMStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StorageComponent, AfterAutoHandleStateEvent>(OnStorageAfterAutoHandle);
    }

    private void OnStorageAfterAutoHandle(Entity<StorageComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _storage.UpdateUI((ent, ent));
    }
}
