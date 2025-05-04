using Content.Server.Chemistry.Components;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Chemistry.ChemMaster;

public sealed partial class RMCChemMasterSystem : SharedRMCChemMasterSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChemMasterComponent, OpenChangePillBottleColorMenuMessage>(OpenChangeBottleColorWindow);
    }

    private void OpenChangeBottleColorWindow(EntityUid ent, ChemMasterComponent comp, OpenChangePillBottleColorMenuMessage args)
    {
        var user = args.Actor;
        var maybeContainer = _itemSlotsSystem.GetItemOrNull(ent, SharedChemMaster.OutputSlotName);
        if (maybeContainer is not { Valid: true } container
                || !HasComp<StorageComponent>(container))
        {
            _popup.PopupEntity(Loc.GetString("rmc-chem-master-non-pill-bottle-output"), ent, user);
            return; // output can't fit pills
        }

        _ui.OpenUi(ent, ChangePillBottleUIKey.Key, user);
    }
}

