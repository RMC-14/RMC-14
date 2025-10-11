using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Refill;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Robust.Server.Audio;

namespace Content.Server._RMC14.Medical;

public sealed class RMCHypospraySystem : RMCSharedHypospraySystem
{
    protected override void OnInteractUsing(Entity<RMCHyposprayComponent> ent, ref InteractUsingEvent args)
    {
        base.OnInteractUsing(ent, ref args);

        if (args.Handled)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.SlotId, out var container))
            return;


        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return;
        // Dont transfer when vial is used
        if (_slots.CanInsert(ent, args.Used, args.User, slots.Slots[ent.Comp.SlotId], true))
            return;

        var vial = container.ContainedEntities[0];

        // Syringe and Spikable handling mostly copied from various places
        // Might be better to convert some stuff to events later

        if (HasComp<InjectorComponent>(args.Used))
        {
            InjectorVialHandling(ent, vial, args.Used, args.User);
            args.Handled = true;
            return;
        }

        if (HasComp<SolutionSpikerComponent>(args.Used))
        {
            SpikableHandling(ent, vial, args.Used, args.User);
            args.Handled = true;
            return;
        }
    }

    // Pretty much a direct copy of the spikablesystem with slight tweaks
    private void SpikableHandling(Entity<RMCHyposprayComponent> ent, EntityUid vial, EntityUid spikable, EntityUid user)
    {
        if (!TryComp<SolutionSpikerComponent>(spikable, out var spike))
            return;

        if (!_solution.TryGetRefillableSolution(vial, out var targetSoln, out var targetSolution)
    || !_solution.TryGetSolution(spikable, spike.SourceSolution, out _, out var sourceSolution))
        {
            return;
        }

        if (targetSolution.Volume == 0 && !spike.IgnoreEmpty)
        {
            _popup.PopupEntity(Loc.GetString(spike.PopupEmpty, ("spiked-entity", vial), ("spike-entity", spikable)), user, user);
            return;
        }

        if (!_solution.ForceAddSolution(targetSoln.Value, sourceSolution))
            return;

        _popup.PopupEntity(Loc.GetString(spike.Popup, ("spiked-entity", vial), ("spike-entity", spikable)), user, user);
        sourceSolution.RemoveAllSolution();
        if (spike.Delete)
            QueueDel(spikable);

        UpdateAppearance(ent);
    }

    private void InjectorVialHandling(Entity<RMCHyposprayComponent> ent, EntityUid vial, EntityUid injector, EntityUid user)
    {
        if (!TryComp<InjectorComponent>(injector, out var syringe))
            return;

        if (!_solution.TryGetSolution(injector, syringe.SolutionName, out var soln, out var solu))
            return;

        Entity<SolutionComponent>? solm;
        Solution? soli;

        if (syringe.ToggleState == InjectorToggleMode.Inject)
        {
            if (!_solution.TryGetInjectableSolution(vial, out solm, out soli))
                return;
        }
        else
        {
            if (!_solution.TryGetDrawableSolution(vial, out solm, out soli))
                return;
        }

        var transferAmount = syringe.ToggleState == InjectorToggleMode.Inject ?
            FixedPoint2.Min(syringe.TransferAmount, soli.AvailableVolume) :
            FixedPoint2.Min(syringe.TransferAmount, solu.AvailableVolume);

        if (transferAmount <= 0)
        {
            if(syringe.ToggleState == InjectorToggleMode.Inject)
                _popup.PopupEntity(Loc.GetString("rmc-hypospray-full", ("vial", vial)), ent, user);
            else
                _popup.PopupEntity(Loc.GetString("rmc-hypospray-full", ("vial", injector)), ent, user);
            return;
        }

        if (syringe.ToggleState == InjectorToggleMode.Draw)
        {
            var removed = _solution.Draw(vial, solm.Value, transferAmount);
            if (!_solution.TryAddSolution(soln.Value, removed))
                return;
            _popup.PopupEntity(Loc.GetString("injector-component-draw-success-message",
    ("amount", removed.Volume),
    ("target", Identity.Entity(vial, EntityManager))), injector, user);
        }
        else
        {
            var adding = _solution.SplitSolution(soln.Value, transferAmount);
            _solution.Inject(vial, solm.Value, adding);
            _popup.PopupEntity(Loc.GetString("injector-component-transfer-success-message",
    ("amount", adding.Volume),
    ("target", Identity.Entity(vial, EntityManager))), injector, user);
        }

        Dirty(soln.Value);
        Dirty(solm.Value);

        UpdateAppearance(ent);
    }
}
