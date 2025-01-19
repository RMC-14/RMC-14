using Content.Server.Chemistry.Components;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Content.Shared.Administration.Notes.AdminMessageEuiState;

namespace Content.Server._RMC14.Chemistry.ChemMaster;

public sealed partial class RMCChemMasterSystem : SharedRMCChemMasterSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChemMasterComponent, OpenChangePillBottleColorMenuMessage>(OpenChangeBottleColorWindow);
        SubscribeLocalEvent<StorageComponent, ChangePillBottleColorMessage>(ChangePillBottleColor);
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

        _ui.OpenUi(container, ChangePillBottleUIKey.Key, user);
    }

    private void ChangePillBottleColor(Entity<StorageComponent> ent, ref ChangePillBottleColorMessage args)
    {
        var pillBottle = ent.Owner;
        _appearance.SetData(pillBottle, PillBottleVisuals.Color, args.NewColor);
    }

}


