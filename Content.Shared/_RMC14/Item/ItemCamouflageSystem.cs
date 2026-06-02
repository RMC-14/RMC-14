using Content.Shared._RMC14.Survivor;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Item;

public sealed class ItemCamouflageSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedStationSpawningSystem _stationSpawning = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public CamouflageType CurrentMapCamouflage { get; set; } = CamouflageType.Jungle;

    private readonly Queue<Entity<ItemCamouflageComponent>> _items = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete, after: [typeof(SurvivorSystem)]);
        SubscribeLocalEvent<ItemCamouflageComponent, MapInitEvent>(OnMapInit);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null)
            return;

        var mob = ev.Mob;

        if (!_prototypes.TryIndex<JobPrototype>(ev.JobId, out var jobPrototype))
            return;

        if (jobPrototype.CamouflageStartingGear == null)
            return;

        if (!jobPrototype.CamouflageStartingGear.TryGetValue(CurrentMapCamouflage, out var startingGear))
            return;

        _prototypes.TryIndex(startingGear, out var gearProto);
        EquipMapCamoGear(mob, gearProto);
    }

    private void EquipMapCamoGear(EntityUid mob, IEquipmentLoadout? startingGear)
    {
        if (startingGear == null)
            return;

        // Delete any existing gear
        var slots = _inventory.GetSlotEnumerator(mob, SlotFlags.All);
        while (slots.NextItem(out var item, out var slot))
        {
            var equipmentStr = startingGear.GetGear(slot.Name);
            if (!string.IsNullOrEmpty(equipmentStr))
            {
                _inventory.TryUnequip(mob, slot.Name, silent: true, force: true, checkDoafter: false);
                QueueDel(item);
            }
        }

        _stationSpawning.EquipStartingGear(mob, startingGear);
    }

    private void OnMapInit(Entity<ItemCamouflageComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        _items.Enqueue(ent);
    }

    public override void Update(float frameTime)
    {
        if (_items.Count == 0)
            return;

        while (_items.TryDequeue(out var ent))
        {
            if (TerminatingOrDeleted(ent))
                continue;

            _appearance.SetData(ent, ItemCamouflageVisuals.Camo, CurrentMapCamouflage);

            if (ent.Comp.CamoNames != null && ent.Comp.CamoNames.TryGetValue(CurrentMapCamouflage, out var camoName))
                _metaData.SetEntityName(ent, camoName);

            if (ent.Comp.CamoDescriptions != null && ent.Comp.CamoDescriptions.TryGetValue(CurrentMapCamouflage, out var camoDescription))
                _metaData.SetEntityDescription(ent, camoDescription);

            if (ent.Comp.States != null && ent.Comp.States.TryGetValue(CurrentMapCamouflage, out var camoState))
                _item.SetHeldPrefix(ent, camoState);
        }
    }
}
