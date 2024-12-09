using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Medical.MedevacStretcher;
using Content.Shared._RMC14.Sprite;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

public abstract partial class SharedMedevacSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DropshipUtilitySystem _dropshipUtility = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMedevacStretcherSystem _stretcherSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedevacComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MedevacComponent, InteractHandEvent>(OnInteract);
        //SubscribeLocalEvent<MedevacComponent, MedevacDoAfterEvent>(OnMedevacDoAfter);
    }

    private void OnInit(Entity<MedevacComponent> ent, ref ComponentInit args)
    {
        _useDelay.SetLength(ent.Owner, ent.Comp.DelayLength, MedevacComponent.AnimationDelay);
    }

    private void OnInteract(Entity<MedevacComponent> ent, ref InteractHandEvent args)
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
            _popup.PopupClient(Loc.GetString("rmc-medevac-no-target"), targetCoord, args.User);
            return;
        }

        var dropshipUtilEnt = (ent.Owner, utilComp);
        //var dropshipUtilPointEnt = (args.Target, utilPointComp);

        if (!_dropshipUtility.IsActivatable(dropshipUtilEnt, args.User, out var popup))
        {
            _popup.PopupClient(popup, targetCoord, args.User);
            return;
        }

        var ev = new PrepareMedevacEvent(_entityManager.GetNetEntity(args.Target));
        RaiseLocalEvent(utilComp.Target.Value, ev);

        if (ev.ReadyForMedevac)
        {
            //_dropshipUtility.ResetActivationCooldown(dropshipUtilEnt);
            _appearanceSystem.SetData(args.Target, DropshipUtilityVisuals.State, MedevacComponent.AnimationState);
            ent.Comp.IsActivated = true;
            _useDelay.TryResetDelay(ent.Owner, id: MedevacComponent.AnimationDelay);
        }
        else
        {
            _popup.PopupClient("rmc-medevac-stretcher-failure", targetCoord, args.User);
        }
    }

    private void OnMedevacDoAfter(Entity<MedevacComponent> ent)
    {
        if (!TryComp(ent.Owner, out DropshipUtilityComponent? utilComp))
        {
            return;
        }

        var target = utilComp.Target;
        if (target is null)
        {
            return;
        }

        if (target is null ||
            !TryComp(target, out MedevacStretcherComponent? stretcherComp))
        {
            return;
        }
        var utilAttachPointEntityUid = utilComp.AttachmentPoint;
        var dropshipUtilEnt = (ent.Owner, utilComp);
        var stretcherEnt = (target.Value, stretcherComp);

        ent.Comp.IsActivated = false;
        if (utilComp.UtilityAttachedSprite is not null &&
            utilAttachPointEntityUid is not null)
        {
            _appearanceSystem.SetData(utilAttachPointEntityUid.Value, DropshipUtilityVisuals.State, utilComp.UtilityAttachedSprite.RsiState);
        }
        _stretcherSystem.Medevac(stretcherEnt, ent.Owner);

        _dropshipUtility.ResetActivationCooldown(dropshipUtilEnt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var medevacQuery = AllEntityQuery<MedevacComponent>();
        while (medevacQuery.MoveNext(out EntityUid ent, out MedevacComponent? medicomp))
        {
            if (!TryComp(ent, out UseDelayComponent? delayComp))
            {
                return;
            }

            if (medicomp.IsActivated &&
                !_useDelay.IsDelayed((ent, delayComp), MedevacComponent.AnimationDelay))
            {
                OnMedevacDoAfter((ent, medicomp));
            }
        }
    }
}
