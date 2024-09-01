using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

public sealed partial class MedivacSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DropshipUtilitySystem _dropshipUtility = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedivacComponent, InteractHandEvent>(OnInteract);

    }

    private void OnInteract(Entity<MedivacComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp(ent.Owner, out DropshipUtilityComponent? utilComp))
        {
            return;
        }
        EntityCoordinates targetCoord = ent.Owner.ToCoordinates();
        if (utilComp.Target is null)
        {
            _popup.PopupClient(Loc.GetString("rmc-medivac-no-target"), targetCoord, args.User);
            return;
        }

        var dropshipUtilEnt = (ent.Owner, utilComp);

        if (!_dropshipUtility.IsActivatable(dropshipUtilEnt, args.User, out var popup))
        {
            _popup.PopupClient(popup, targetCoord, args.User);
            return;
        }

        var medivacNetCoordinates = _entityManager.GetNetCoordinates(ent.Owner.ToCoordinates().SnapToGrid(_entityManager, _mapManager));
        var ev = new MedivacEvent(medivacNetCoordinates);
        RaiseLocalEvent(utilComp.Target.Value, ev);

        if (ev.SucessfulMedivac)
        {
            _dropshipUtility.ResetActivationCooldown(dropshipUtilEnt);
        }
    }
}
