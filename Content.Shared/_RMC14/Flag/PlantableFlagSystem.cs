using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.CombatMode;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Flag;

public sealed class PlantableFlagSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;

    private readonly HashSet<EntProtoId<IFFFactionComponent>> _userFactions = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantableFlagComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PlantableFlagComponent, PlantableFlagPlantDoAfterEvent>(OnPlantDoAfter);
        SubscribeLocalEvent<PlantableFlagComponent, PlantableFlagRemoveDoAfterEvent>(OnRemoveDoAfter);
        SubscribeLocalEvent<PlantableFlagComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
    }

    private void OnUseInHand(Entity<PlantableFlagComponent> ent, ref UseInHandEvent args)
    {
        if (!CanPlantFlagPopup(ent, args.User, out _))
            return;

        args.Handled = true;
        var ev = new PlantableFlagPlantDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent, ent, ent)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _audio.PlayPredicted(ent.Comp.RaiseStartSound, ent, args.User);
    }

    private void OnPlantDoAfter(Entity<PlantableFlagComponent> ent, ref PlantableFlagPlantDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        if (!CanPlantFlagPopup(ent, args.User, out var target))
            return;

        _transform.SetCoordinates(ent, target.Value);
        _transform.SetLocalRotation(ent, Angle.Zero);
        _transform.AnchorEntity(ent);

        _appearance.SetData(ent, PlantableFlagVisuals.Planted, true);

        if (ent.Comp.DeployOffset != Vector2.Zero)
            _rmcSprite.SetOffset(ent, ent.Comp.DeployOffset);

        if (_net.IsClient)
            return;

        var sound = ent.Comp.RaiseEndSound;
        if (_combatMode.IsInCombatMode(args.User))
        {
            sound = ent.Comp.RaisedCombatSound;
            if (TryComp(args.User, out UserIFFComponent? userIff) &&
                _gunIFF.TryGetFactions((args.User, userIff), _userFactions))
            {
                var allies = 0;
                var inRange = _entityLookup.GetEntitiesInRange<UserIFFComponent>(args.User.ToCoordinates(), ent.Comp.AlliesRange);
                foreach (var inRangeEnt in inRange)
                {
                    foreach (var faction in _userFactions)
                    {
                        if (_gunIFF.IsInFaction((inRangeEnt.Owner, inRangeEnt.Comp), faction))
                        {
                        allies++;
                            break;
                        }
                    }

                    if (allies >= ent.Comp.AlliesRequired)
                    {
                        sound = ent.Comp.RaisedCombatAlliesSound;
                        break;
                    }
                }
            }
        }

        _audio.PlayPvs(sound, ent);
    }

    private void OnRemoveDoAfter(Entity<PlantableFlagComponent> ent, ref PlantableFlagRemoveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        _transform.Unanchor(ent);
        _hands.TryPickupAnyHand(args.User, ent);
        _appearance.SetData(ent, PlantableFlagVisuals.Planted, false);
        _rmcSprite.SetOffset(ent, Vector2.Zero);
    }

    private void OnGetAlternativeVerbs(Entity<PlantableFlagComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp(ent, out TransformComponent? transform) ||
            !transform.Anchored)
        {
            return;
        }

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Take Down",
            Act = () =>
            {
                var ev = new PlantableFlagRemoveDoAfterEvent();
                var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.Delay, ev, ent, ent, ent)
                {
                    BreakOnMove = true,
                    NeedHand = true,
                };

                if (_doAfter.TryStartDoAfter(doAfter))
                    _audio.PlayPredicted(ent.Comp.LowerStartSound, ent, user);
            },
        });
    }

    private bool CanPlantFlagPopup(Entity<PlantableFlagComponent> ent, EntityUid user, [NotNullWhen(true)] out EntityCoordinates? target)
    {
        target = null;
        if (!TryComp(user, out TransformComponent? userTransform))
            return false;

        var (coords, rot) = _transform.GetMoverCoordinateRotation(user, userTransform);
        target = coords.Offset(rot.ToWorldVec());
        if (_rmcMap.IsTileBlocked(target.Value))
        {
            _popup.PopupClient(
                $"You need a clear, open area to plant the {Name(ent)}, something is blocking the way in front of you!",
                user,
                user,
                PopupType.MediumCaution
            );

            return false;
        }

        return true;
    }
}
