using Content.Server._CM14.Webbing;
using Content.Server.Storage.EntitySystems;
using Content.Shared._CM14.Webbing;
using Content.Shared.Explosion;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Storage;

namespace Content.Server.Inventory
{
    public sealed class ServerInventorySystem : InventorySystem
    {
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly WebbingSystem _webbing = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InventoryComponent, BeforeExplodeEvent>(OnExploded);
            SubscribeNetworkEvent<OpenSlotStorageNetworkMessage>(OnOpenSlotStorage);
        }

        private void OnExploded(Entity<InventoryComponent> ent, ref BeforeExplodeEvent args)
        {
            // explode each item in their inventory too
            var slots = new InventorySlotEnumerator(ent);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null)
                    args.Contents.Add(slot.ContainedEntity.Value);
            }
        }

        private void OnOpenSlotStorage(OpenSlotStorageNetworkMessage ev, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
                    return;

            if (TryGetSlotEntity(uid, ev.Slot, out var entityUid))
            {
                if (TryComp<StorageComponent>(entityUid, out var storageComponent))
                    _storageSystem.OpenStorageUI(entityUid.Value, uid, storageComponent);
                else if (TryComp<WebbingClothingComponent>(entityUid, out var webbingClothing))
                    _webbing.OpenStorage((entityUid.Value, webbingClothing), uid);
            }
        }

        public void TransferEntityInventories(Entity<InventoryComponent?> source, Entity<InventoryComponent?> target)
        {
            if (!Resolve(source.Owner, ref source.Comp) || !Resolve(target.Owner, ref target.Comp))
                return;

            var enumerator = new InventorySlotEnumerator(source.Comp);
            while (enumerator.NextItem(out var item, out var slot))
            {
                if (TryUnequip(source, slot.Name, true, true, inventory: source.Comp))
                    TryEquip(target, item, slot.Name , true, true, inventory: target.Comp);
            }
        }
    }
}
