using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.MedevacStretcher;
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
using Robust.Shared.Audio.Systems;
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

namespace Content.Shared._RMC14.Medical.MedevacStretcher;

public abstract partial class SharedMedevacStretcherSystem : EntitySystem
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public const int MinimumRequiredSkill = 2;
    public static readonly EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillMedical";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedevacStretcherComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MedevacStretcherComponent, GetVerbsEvent<InteractionVerb>>(AddActivateBeaconVerb);
        SubscribeLocalEvent<MedevacStretcherComponent, FoldedEvent>(OnFold);
        SubscribeLocalEvent<MedevacStretcherComponent, PrepareMedevacEvent>(PrepareMedevac);
        SubscribeLocalEvent<MedevacStretcherComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MedevacStretcherComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<MedevacStretcherComponent, StrapAttemptEvent>(OnTryStrap);
    }

    public void Medevac(Entity<MedevacStretcherComponent> ent, EntityUid medevacEntity)
    {
        if (!TryComp(ent.Owner, out StrapComponent? strapComp))
        {
            return;
        }
        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedevacStretcherComponent.BuckledSlotId);

        if (slot.ContainedEntity is not { } buckled)
        {
            return;
        }
        _transformSystem.PlaceNextTo(buckled, medevacEntity);
        DeactivateBeacon(ent.Owner);
        _appearance.SetData(ent.Owner, StrapVisuals.State, false);
    }

    private void Unstrap(Entity<MedevacStretcherComponent> ent, EntityUid user)
    {
        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedevacStretcherComponent.BuckledSlotId);
        if (slot.ContainedEntity is null)
        {
            return;
        }

        if (!TryComp(ent.Owner, out StrapComponent? strapComp))
        {
            return;
        }
        _audio.PlayPredicted(strapComp.UnbuckleSound, ent.Owner, user);

        _appearance.SetData(ent.Owner, StrapVisuals.State, false);
        _container.Remove(slot.ContainedEntity.Value, slot);
    }

    private void OnInit(Entity<MedevacStretcherComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Id = _dropshipWeaponSystem.ComputeNextId();
    }

    private void OnExamine(Entity<MedevacStretcherComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(MedevacStretcherComponent)))
        {
            args.PushText(Loc.GetString("rmc-medevac-stretcher-examine-id", ("id", ent.Comp.Id)));
        }
    }

    private void AddActivateBeaconVerb(Entity<MedevacStretcherComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
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
            Text = Loc.GetString("rmc-medevac-activate-beacon-verb"),
            Act = () =>
            {
                ActivateBeacon(@event.Target, @event.User);
            },
            Priority = 1
        });
        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedevacStretcherComponent.BuckledSlotId);
        if (slot.Count > 0)
        {
            args.Verbs.Add(new InteractionVerb()
            {
                Text = Loc.GetString("verb-categories-unbuckle"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unbuckle.svg.192dpi.png")),
                Act = () =>
                {
                    Unstrap(ent, @event.User);
                },
                Priority = 1
            });
        }
    }

    private void OnFold(Entity<MedevacStretcherComponent> ent, ref FoldedEvent args)
    {
        if (args.IsFolded)
        {
            DeactivateBeacon(ent.Owner);
        }
    }

    private void PrepareMedevac(Entity<MedevacStretcherComponent> ent, ref PrepareMedevacEvent args)
    {
        if (!TryComp(ent.Owner, out StrapComponent? strapComp))
        {
            return;
        }

        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedevacStretcherComponent.BuckledSlotId);

        if (slot.ContainedEntity is not { } buckled)
        {
            return;
        }
        _appearance.SetData(ent.Owner, MedevacStretcherVisuals.MedevacingState, true);
        args.ReadyForMedevac = true;
    }

    private void OnInteract(Entity<MedevacStretcherComponent> ent, ref InteractHandEvent args)
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

    private void OnTryStrap(Entity<MedevacStretcherComponent> ent, ref StrapAttemptEvent args)
    {
        var slot = _container.EnsureContainer<ContainerSlot>(ent.Owner, MedevacStretcherComponent.BuckledSlotId);
        _container.Insert(args.Buckle.Owner, slot);
        _appearance.SetData(ent.Owner, StrapVisuals.State, true);

        if (!TryComp(ent.Owner, out StrapComponent? strapComp))
        {
            return;
        }
        _audio.PlayPredicted(strapComp.BuckleSound, args.Strap.Owner, args.User);
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
            (!_areas.TryGetArea(stretcher.ToCoordinates().SnapToGrid(_entites, _mapManager), out _, out var stretcherArea) ||
            !stretcherArea.Medevac))
        {
            _popup.PopupClient(Loc.GetString("rmc-medevac-area-not-cas"), stretcherCoords, user);
            return false;
        }

        var slot = _container.EnsureContainer<ContainerSlot>(stretcher, MedevacStretcherComponent.BuckledSlotId);
        if (!TryComp(stretcher, out StrapComponent? strapComp) ||
            slot.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-medevac-area-no-patient"), stretcherCoords, user);
            return false;
        }

        if (!TryComp(stretcher, out MedevacStretcherComponent? stretcherComp))
        {
            return false;
        }

        var targetComp = new DropshipTargetComponent()
        {
            Abbreviation = GetMedevacAbbreviation(stretcherComp.Id),
            IsTargetableByWeapons = false
        };
        AddComp(stretcher, targetComp, true);
        _appearance.SetData(stretcher, MedevacStretcherVisuals.BeaconState, BeaconVisuals.On);
        _popup.PopupClient(Loc.GetString("rmc-medevac-activate-beacon"), stretcherCoords, user);
        return true;
    }

    private bool DeactivateBeacon(EntityUid stretcher)
    {
        if (!HasComp<DropshipTargetComponent>(stretcher))
        {
            return true;
        }
        RemCompDeferred<DropshipTargetComponent>(stretcher);
        _appearance.SetData(stretcher, MedevacStretcherVisuals.BeaconState, BeaconVisuals.Off);
        _appearance.SetData(stretcher, MedevacStretcherVisuals.MedevacingState, false);
        return true;
    }

    private string GetMedevacAbbreviation(int id)
    {
        return Loc.GetString("rmc-medevac-target-abbreviation", ("id", id));
    }

}

[Serializable, NetSerializable]
public enum MedevacStretcherVisuals : byte
{
    BeaconState,
    MedevacingState
}

[Serializable, NetSerializable]
public enum BeaconVisuals : byte
{
    Off,
    On
}
