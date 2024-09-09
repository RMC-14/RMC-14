using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Medical.MedivacStretcher;

public abstract partial class SharedMedivacStretcherSystem : EntitySystem
{
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityManager _entites = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeaponSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public const int MinimumRequiredSkill = 2;
    public static readonly EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillMedical";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedivacStretcherComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MedivacStretcherComponent, GetVerbsEvent<InteractionVerb>>(AddActivateBeaconVerb);
        SubscribeLocalEvent<MedivacStretcherComponent, FoldedEvent>(OnFold);
        SubscribeLocalEvent<MedivacStretcherComponent, PrepareMedivacEvent>(PrepareMedivac);
        SubscribeLocalEvent<MedivacStretcherComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MedivacStretcherComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<MedivacStretcherComponent, StrapAttemptEvent>(OnTryStrap);
    }

    public void Medivac(Entity<MedivacStretcherComponent> ent, EntityUid medivacEntiyt)
    {
        if (!TryComp(ent.Owner, out StrapComponent? strapComp))
        {
            return;
        }
        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedivacStretcherComponent.BuckledSlotId);

        if (slot.ContainedEntity is not { } buckled)
        {
            return;
        }
        _transformSystem.PlaceNextTo(buckled, medivacEntiyt);
        RemCompDeferred<DropshipTargetComponent>(ent);
        _appearance.SetData(ent.Owner, MedivacStretcherVisuals.BeaconState, BeaconVisuals.Off);
        _appearance.SetData(ent.Owner, MedivacStretcherVisuals.MedivacingState, false);
    }

    private void Unstrap(Entity<MedivacStretcherComponent> ent)
    {
        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedivacStretcherComponent.BuckledSlotId);
        if (slot.ContainedEntity is null)
        {
            return;
        }
        _appearance.SetData(ent.Owner, StrapVisuals.State, false);
        _container.Remove(slot.ContainedEntity.Value, slot);
    }

    private void OnInit(Entity<MedivacStretcherComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Id = _dropshipWeaponSystem.ComputeNextId();
    }

    private void OnExamine(Entity<MedivacStretcherComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(MedivacStretcherComponent)))
        {
            args.PushText(Loc.GetString("rmc-medivac-stretcher-examine-id", ("id", ent.Comp.Id)));
        }
    }

    private void AddActivateBeaconVerb(Entity<MedivacStretcherComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var (uid, comp) = ent;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        if (!_skills.HasSkill(args.User, SkillType, MinimumRequiredSkill))
        {
            return;
        }

        var @event = args;
        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("rmc-medivac-activate-beacon-verb"),
            Act = () =>
            {
                ActivateBeacon(@event.Target, @event.User);
            },
            Priority = 1
        });
        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedivacStretcherComponent.BuckledSlotId);
        if (slot.Count > 0)
        {
            args.Verbs.Add(new InteractionVerb()
            {
                Text = Loc.GetString("verb-categories-unbuckle"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unbuckle.svg.192dpi.png")),
                Act = () =>
                {
                    Unstrap(ent);
                },
                Priority = 1
            });
        }
    }

    private void OnFold(Entity<MedivacStretcherComponent> ent, ref FoldedEvent args)
    {
        if (args.IsFolded)
        {
            DeactivateBeacon(ent.Owner);
        }
    }

    private void PrepareMedivac(Entity<MedivacStretcherComponent> ent, ref PrepareMedivacEvent args)
    {
        if (!TryComp(ent.Owner, out StrapComponent? strapComp))
        {
            return;
        }

        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedivacStretcherComponent.BuckledSlotId);

        if (slot.ContainedEntity is not { } buckled)
        {
            return;
        }
        _appearance.SetData(ent.Owner, MedivacStretcherVisuals.MedivacingState, true);
        args.ReadyForMedivac = true;
    }

    private void OnInteract(Entity<MedivacStretcherComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        if (TryComp(ent.Owner, out FoldableComponent? foldComp) &&
            foldComp.IsFolded)
        {
            return;
        }

        ToggleBeacon(args.Target, args.User);
        args.Handled = true;
    }

    private void OnTryStrap(Entity<MedivacStretcherComponent> ent, ref StrapAttemptEvent args)
    {
        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedivacStretcherComponent.BuckledSlotId);
        _container.Insert(args.Buckle.Owner, slot);
        _appearance.SetData(ent.Owner, StrapVisuals.State, true);
        args.Cancelled = true;
    }

    private void ToggleBeacon(EntityUid stretcher, EntityUid user)
    {
        if (HasComp<DropshipTargetComponent>(stretcher))
        {
            DeactivateBeacon(stretcher);
        }
        else
        {
            ActivateBeacon(stretcher, user);
        }
    }

    private bool ActivateBeacon(EntityUid stretcher, EntityUid user)
    {
        if (HasComp<DropshipTargetComponent>(stretcher))
        {
            return true;
        }
        EntityCoordinates stretcherCoords = stretcher.ToCoordinates();
        if (!_dropshipWeaponSystem.CasDebug &&
            !_areas.CanCAS(stretcher.ToCoordinates().SnapToGrid(_entites, _mapManager)))
        {
            _popup.PopupClient(Loc.GetString("rmc-medivac-area-not-cas"), stretcherCoords, user);
            return false;
        }

        var slot = _container.EnsureContainer<ContainerSlot>(stretcher, MedivacStretcherComponent.BuckledSlotId);
        if (!TryComp(stretcher, out StrapComponent? strapComp) ||
            slot.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-medivac-area-no-patient"), stretcherCoords, user);
            return false;
        }

        if (!TryComp(stretcher, out MedivacStretcherComponent? stretcherComp))
        {
            return false;
        }

        var targetComp = new DropshipTargetComponent()
        {
            Abbreviation = GetMedivacAbbreviation(stretcherComp.Id),
            IsTargetableByWeapons = false
        };
        AddComp(stretcher, targetComp, true);
        _appearance.SetData(stretcher, MedivacStretcherVisuals.BeaconState, BeaconVisuals.On);
        _popup.PopupClient(Loc.GetString("rmc-medivac-activate-beacon"), stretcherCoords, user);
        return true;
    }

    private bool DeactivateBeacon(EntityUid stretcher)
    {
        if (!HasComp<DropshipTargetComponent>(stretcher))
        {
            return true;
        }
        RemCompDeferred<DropshipTargetComponent>(stretcher);
        _appearance.SetData(stretcher, MedivacStretcherVisuals.BeaconState, BeaconVisuals.Off);
        _appearance.SetData(stretcher, MedivacStretcherVisuals.MedivacingState, false);
        return true;
    }

    private string GetMedivacAbbreviation(int id)
    {
        return Loc.GetString("rmc-medivac-target-abbreviation", ("id", id));
    }

}

[Serializable, NetSerializable]
public enum MedivacStretcherVisuals : byte
{
    BeaconState,
    MedivacingState
}

[Serializable, NetSerializable]
public enum BeaconVisuals : byte
{
    Off,
    On
}
