using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Armor;

public sealed class RMCArmorSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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

        var profile = actor.PlayerSession != null
            ? _prefs.GetPreferences(actor.PlayerSession.UserId).SelectedCharacter as HumanoidCharacterProfile
            : HumanoidCharacterProfile.RandomWithSpecies();

        if (!_armorVariantQuery.TryComp(args.Item, out var armor))
            return;

        if (profile == null)
            return;

        var armorType = profile.ArmorPreference;
        var equipmentEntityID = armor.DefaultType;

        if (armor.Types.TryGetValue(armorType.ToString(), out var equipment))
            equipmentEntityID = equipment;

        if (armorType == ArmorPreference.Random)
        {
            var random = new Random();
            var randomType = armor.Types.ElementAt(random.Next(0, armor.Types.Count)).Value;
            equipmentEntityID = randomType;
        }

        var equipmentEntity = Spawn(equipmentEntityID, _transform.GetMapCoordinates(ent));
        InventorySystem.TryEquip(ent, equipmentEntity, "outerClothing", force: true, predicted: false);

        QueueDel(args.Item);
    }
}
