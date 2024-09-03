using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Dropship.Utility;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Medical.MedivacStretcher;

public sealed partial class MedivacStretcherSystem : EntitySystem
{
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityManager _entites = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeaponSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public const int MinimumRequiredSkill = 2;
    public static readonly EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillMedical";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedivacStretcherComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MedivacStretcherComponent, GetVerbsEvent<InteractionVerb>>(AddActivateBeaconVerb);
        SubscribeLocalEvent<MedivacStretcherComponent, FoldedEvent>(OnFold);
        SubscribeLocalEvent<MedivacStretcherComponent, MedivacEvent>(OnMedivac);
        SubscribeLocalEvent<MedivacStretcherComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MedivacStretcherComponent, InteractHandEvent>(OnInteract);
    }

    private void OnInit(Entity<MedivacStretcherComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Id = _dropshipWeaponSystem.GetNextTargetID();
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
            //Icon = new SpriteSpecifier(),
            Act = () =>
            {
                ActivateBeacon(@event.Target, @event.User);
            },
            Priority = 1
        });
    }

    private void OnFold(Entity<MedivacStretcherComponent> ent, ref FoldedEvent args)
    {
        if (args.IsFolded)
        {
            RemCompDeferred<DropshipTargetComponent>(ent);
        }
    }

    private void OnMedivac(Entity<MedivacStretcherComponent> ent, ref MedivacEvent args)
    {
        if (!TryComp(ent.Owner, out StrapComponent? strapComp))
        {
            return;
        }

        if (!strapComp.BuckledEntities.TryFirstOrNull(out EntityUid? buckled))
        {
            return;
        }
        //_transformSystem.SetCoordinates(buckled.Value, medivacCordinates);
        _transformSystem.PlaceNextTo(buckled.Value, _entites.GetEntity(args.MedivacEntity));
        args.SucessfulMedivac = true;

        RemCompDeferred<DropshipTargetComponent>(ent);
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

        ActivateBeacon(args.Target, args.User);
        args.Handled = true;
    }

    private void ActivateBeacon(EntityUid stretcher, EntityUid user)
    {
        EntityCoordinates stretcherCoords = stretcher.ToCoordinates();
        if (!_areas.CanCAS(stretcher.ToCoordinates().SnapToGrid(_entites, _mapManager)))
        {
            _popup.PopupClient(Loc.GetString("rmc-medivac-area-not-cas"), stretcherCoords, user);
        }

        if (!TryComp(stretcher, out StrapComponent? strapComp) ||
            !strapComp.BuckledEntities.Any())
        {
            _popup.PopupClient(Loc.GetString("rmc-medivac-area-no-patient"), stretcherCoords, user);
            return;
        }

        if (!TryComp(stretcher, out MedivacStretcherComponent? stretcherComp))
        {
            return;
        }

        var targetComp = new DropshipTargetComponent()
        {
            Abbreviation = GetMedivacAbbreviation(stretcherComp.Id),
            IsTargetableByWeapons = false
        };
        AddComp(stretcher, targetComp, true);

        _popup.PopupClient(Loc.GetString("rmc-medivac-activate-beacon"), stretcherCoords, user);
    }

    private string GetMedivacAbbreviation(int id)
    {
        return Loc.GetString("rmc-medivac-target-abbreviation", ("id", id));
    }
}
