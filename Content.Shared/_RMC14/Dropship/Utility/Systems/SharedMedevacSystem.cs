using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Utility.Events;
using Content.Shared._RMC14.Medical.MedevacStretcher;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Dropship.Utility.Systems;

public abstract class SharedMedevacSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DropshipUtilitySystem _dropshipUtility = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MedevacStretcherSystem _stretcher = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MedevacComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MedevacComponent, InteractHandEvent>(OnInteract);
    }

    private void OnMapInit(Entity<MedevacComponent> ent, ref MapInitEvent args)
    {
        _useDelay.SetLength(ent.Owner, ent.Comp.DelayLength, MedevacComponent.AnimationDelay);
    }

    private void OnInteract(Entity<MedevacComponent> ent, ref InteractHandEvent args)
    {
        // This component should only be called via the utility attachement point passing the
        // InteractHandEvent. Direct intraction with this component should be ignored
        if (args.Target == ent.Owner)
            return;

        if (ent.Comp.IsActivated)
            return;

        if (!TryComp(ent.Owner, out DropshipUtilityComponent? utilComp) ||
            !HasComp<DropshipUtilityPointComponent>(args.Target))
        {
            return;
        }

        var targetCoord = ent.Owner.ToCoordinates();
        if (utilComp.Target == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-medevac-no-target"), targetCoord, args.User);
            return;
        }

        var dropshipUtilEnt = new Entity<DropshipUtilityComponent>(ent.Owner, utilComp);
        if (!_dropshipUtility.IsActivatable(dropshipUtilEnt, args.User, out var popup))
        {
            if (_net.IsServer)
                _popup.PopupCoordinates(popup, targetCoord, args.User);

            return;
        }

        var ev = new PrepareMedevacEvent(GetNetEntity(args.Target));
        RaiseLocalEvent(utilComp.Target.Value, ev);

        if (ev.ReadyForMedevac)
        {
            _appearance.SetData(args.Target, DropshipUtilityVisuals.State, MedevacComponent.AnimationState);
            ent.Comp.IsActivated = true;
            _useDelay.TryResetDelay(ent.Owner, id: MedevacComponent.AnimationDelay);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("rmc-medevac-stretcher-failure"), targetCoord, args.User);
        }
    }

    private void AfterMedevac(Entity<MedevacComponent> ent)
    {
        if (!TryComp(ent.Owner, out DropshipUtilityComponent? utilComp))
            return;

        var target = utilComp.Target;
        if (target == null)
            return;

        if (!TryComp(target, out MedevacStretcherComponent? stretcherComp))
            return;

        var utilId = utilComp.AttachmentPoint;
        var utilEnt = (ent.Owner, utilComp);
        var stretcherEnt = (target.Value, stretcherComp);

        ent.Comp.IsActivated = false;
        if (TryComp(ent, out DropshipAttachedSpriteComponent? sprite) &&
            sprite.Sprite != null &&
            utilId != null)
        {
            _appearance.SetData(utilId.Value, DropshipUtilityVisuals.State, sprite.Sprite.RsiState);
        }

        _stretcher.Medevac(stretcherEnt, ent.Owner);
        _dropshipUtility.ResetActivationCooldown(utilEnt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var medevacQuery = AllEntityQuery<MedevacComponent>();
        while (medevacQuery.MoveNext(out var ent, out var medicomp))
        {
            if (!TryComp(ent, out UseDelayComponent? delayComp))
                return;

            if (medicomp.IsActivated &&
                !_useDelay.IsDelayed((ent, delayComp), MedevacComponent.AnimationDelay))
            {
                AfterMedevac((ent, medicomp));
            }
        }
    }
}
