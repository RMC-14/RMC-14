using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Dropship.Matrix;

public sealed class DropshipMatrixSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeapon = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipMatrixComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DropshipMatrixComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DropshipMatrixComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInteractUsing(Entity<DropshipMatrixComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.State == DropshipMatrixState.Empty)
        {
            if (!HasComp<VialComponent>(args.Used))
                return;

            args.Handled = true;
            TryInsertVial(ent, args.Used, args.User);
            return;
        }

        if (!_tool.HasQuality(args.Used, ent.Comp.LockQuality))
            return;

        args.Handled = true;
        var locking = ent.Comp.State == DropshipMatrixState.Unlocked;
        ent.Comp.State = locking ? DropshipMatrixState.Locked : DropshipMatrixState.Unlocked;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.LockSound, ent, args.User);
        var message = locking ? "rmc-dropship-matrix-locked" : "rmc-dropship-matrix-unlocked";
        _popup.PopupClient(Loc.GetString(message), ent, args.User);
    }

    private void TryInsertVial(Entity<DropshipMatrixComponent> ent, EntityUid vial, EntityUid user)
    {
        if (!_solution.TryGetSolution(vial, ent.Comp.VialSolution, out _, out var solution))
            return;

        if (solution.Volume < ent.Comp.RequiredVolume)
        {
            _popup.PopupClient(Loc.GetString("rmc-dropship-matrix-vial-not-full"), ent, user, PopupType.SmallCaution);
            return;
        }

        if (solution.Contents.Count != 1)
        {
            _popup.PopupClient(Loc.GetString("rmc-dropship-matrix-vial-not-pure"), ent, user, PopupType.SmallCaution);
            return;
        }

        var upgrade = GetUpgrade(solution.Contents[0].Reagent.Prototype);
        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        if (!_container.Insert(vial, container))
            return;

        ent.Comp.Upgrade = upgrade;
        ent.Comp.State = DropshipMatrixState.Unlocked;
        Dirty(ent);

        _popup.PopupClient(Loc.GetString("rmc-dropship-matrix-vial-added"), ent, user);
    }

    private DropshipMatrixUpgrade GetUpgrade(string reagent)
    {
        if (_reagent.HasProperty<Crystallization>(reagent))
            return DropshipMatrixUpgrade.Wide;

        if (_reagent.HasProperty<Photosensitive>(reagent))
            return DropshipMatrixUpgrade.NightVision;

        return DropshipMatrixUpgrade.None;
    }

    private void OnAfterInteract(Entity<DropshipMatrixComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            !args.CanReach ||
            args.Target is not { } target ||
            !TryComp(target, out DropshipTerminalWeaponsComponent? terminal))
        {
            return;
        }

        args.Handled = true;
        if (ent.Comp.State != DropshipMatrixState.Locked)
        {
            _popup.PopupClient(Loc.GetString("rmc-dropship-matrix-incomplete"), target, args.User, PopupType.SmallCaution);
            return;
        }

        _dropshipWeapon.SetMatrix((target, terminal), ent.Comp.Upgrade);
        _popup.PopupClient(Loc.GetString("rmc-dropship-matrix-installed"), target, args.User);
        PredictedQueueDel(ent.Owner);
    }

    private void OnExamined(Entity<DropshipMatrixComponent> ent, ref ExaminedEvent args)
    {
        var message = ent.Comp.State switch
        {
            DropshipMatrixState.Unlocked => "rmc-dropship-matrix-examine-unlocked",
            DropshipMatrixState.Locked => "rmc-dropship-matrix-examine-locked",
            _ => null,
        };

        if (message == null)
            return;

        using (args.PushGroup(nameof(DropshipMatrixComponent)))
        {
            args.PushMarkup(Loc.GetString(message));
        }
    }
}
