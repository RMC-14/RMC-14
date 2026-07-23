using Content.Shared._RMC14.Marines.Armor;
using Content.Shared.Inventory;
using Content.Shared.Light;
using Content.Shared.Verbs;

namespace Content.Shared._RMC14.Armor;

public sealed class RMCSuitLightVerbSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSuitLightComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs,
            after: [typeof(SharedHandheldLightSystem)]);
    }

    private void OnGetVerbs(Entity<RMCSuitLightComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (_inventory.InSlotWithFlags((ent, null, null), SlotFlags.OUTERCLOTHING))
            return;

        var text = Loc.GetString("verb-common-toggle-light");
        args.Verbs.RemoveWhere(verb => verb.Text == text);
    }
}
