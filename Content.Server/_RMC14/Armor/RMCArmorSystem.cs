using System.Linq;
using Content.Server._RMC14.Preferences;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Inventory;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Armor;

public sealed class RMCArmorSystem : EntitySystem
{
    [Dependency] private readonly RMCCharacterSelectionSystem _characters = default!;
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly CMArmorSystem _armorSystem = default!;

    private EntityQuery<RMCArmorVariantComponent> _armorVariantQuery;

    public override void Initialize()
    {
        _armorVariantQuery = GetEntityQuery<RMCArmorVariantComponent>();

        SubscribeLocalEvent<MarineComponent, RMCAutomatedVendedUserEvent>(OnAutomatedVenderUser);
    }

    private void OnAutomatedVenderUser(Entity<MarineComponent> ent, ref RMCAutomatedVendedUserEvent args)
    {
        if (!TryComp(ent, out ActorComponent? actor))
            return;

        if (!_armorVariantQuery.TryComp(args.Item, out var armor))
            return;

        var profile = _characters.GetActiveProfile(actor.PlayerSession);
        var equipmentEntityID = _armorSystem.GetArmorVariant((args.Item, armor), profile.ArmorPreference);
        var equipmentEntity = Spawn(equipmentEntityID, _transform.GetMapCoordinates(ent));
        InventorySystem.TryEquip(ent, equipmentEntity, "outerClothing", force: true, predicted: false);

        var ev = new RMCArmorVariantCreatedEvent(ent, equipmentEntity);
        RaiseLocalEvent(ent, ref ev);

        QueueDel(args.Item);
    }
}
