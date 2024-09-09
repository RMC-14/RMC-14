using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.Dropship.Utility.Events;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Medical.MedivacStretcher;
using Content.Shared._RMC14.Sprite;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

public abstract partial class SharedMedivacSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DropshipUtilitySystem _dropshipUtility = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMedivacStretcherSystem _stretcherSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedivacComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<MedivacComponent, MedivacDoAfterEvent>(OnMedivacDoAfter);
    }

    private void OnInteract(Entity<MedivacComponent> ent, ref InteractHandEvent args)
    {
        // This component should only be called via the utility attachement point passing the
        // InteractHandEvent. Direct intraction with this component should be ignored
        if (args.Target == ent.Owner)
        {
            return;
        }

        if (ent.Comp.IsActivated)
        {
            return;
        }

        if (!TryComp(ent.Owner, out DropshipUtilityComponent? utilComp) ||
            !TryComp(args.Target, out DropshipUtilityPointComponent? utilPointComp))
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
        //var dropshipUtilPointEnt = (args.Target, utilPointComp);

        if (!_dropshipUtility.IsActivatable(dropshipUtilEnt, args.User, out var popup))
        {
            _popup.PopupClient(popup, targetCoord, args.User);
            return;
        }

        var ev = new PrepareMedivacEvent(_entityManager.GetNetEntity(args.Target));
        RaiseLocalEvent(utilComp.Target.Value, ev);

        if (ev.ReadyForMedivac)
        {
            //_dropshipUtility.ResetActivationCooldown(dropshipUtilEnt);
            _appearanceSystem.SetData(args.Target, DropshipUtilityVisuals.State, MedivacComponent.AnimationState);
            ent.Comp.IsActivated = true;
            var doAfterEv = new MedivacDoAfterEvent(_entityManager.GetNetEntity(args.Target));
            var doAfterArgs = new DoAfterArgs(_entityManager, args.User, 3.0f, doAfterEv, ent.Owner, utilComp.Target.Value)
            {
                BreakOnMove = false,
                RequireCanInteract = false,
                NeedHand = false,
                Hidden = true,
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
        }
        else
        {
            _popup.PopupClient("rmc-medivac-stretcher-failure", targetCoord, args.User);
        }
    }

    private void OnMedivacDoAfter(Entity<MedivacComponent> ent, ref MedivacDoAfterEvent args)
    {
        if (!TryComp(ent.Owner, out DropshipUtilityComponent? utilComp))
        {
            return;
        }

        if (args.Target is null ||
            !TryComp(args.Target, out MedivacStretcherComponent? stretcherComp))
        {
            return;
        }
        var utilAttachPointEntityUid = _entityManager.GetEntity(args.UtilityAttachmentPoint);
        var dropshipUtilEnt = (ent.Owner, utilComp);
        var stretcherEnt = (args.Target.Value, stretcherComp);

        ent.Comp.IsActivated = false;
        if (utilComp.UtilityAttachedSprite is not null)
        {
            _appearanceSystem.SetData(utilAttachPointEntityUid, DropshipUtilityVisuals.State, utilComp.UtilityAttachedSprite.RsiState);
        }
        _stretcherSystem.Medivac(stretcherEnt, ent.Owner);

        _dropshipUtility.ResetActivationCooldown(dropshipUtilEnt);
    }
}
