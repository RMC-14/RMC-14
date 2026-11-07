using Content.Shared._RMC14.Power;
using Content.Shared.Item;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;

namespace Content.Shared._RMC14.Vendors;

public abstract partial class SharedCMAutomatedVendorSystem
{
    [Dependency] private readonly SharedRMCPowerSystem _requisitionPower = default!;

    public static bool IsRequisitionEntry(CMVendorSection section, CMVendorEntry entry)
    {
        return entry.Points == null && entry.Spawn == 1 && entry.LinkedEntries.Count == 0 &&
               entry.GiveSquadRoleName == null && entry.GiveIcon == null && entry.GivePrefix == null &&
               entry.GiveMapBlip == null &&
               section.Choices == null && section.TakeAll == null && section.TakeOne == null &&
               section.SharedSpecLimit == null && section.Jobs.Count == 0 && section.Ranks.Count == 0 &&
               section.Holidays.Count == 0;
    }

    public int GetRequisitionAmount(
        Entity<CMAutomatedVendorComponent> vendor, CMVendorSection section, CMVendorEntry entry)
    {
        if (!TryGetRequisitionSource(vendor, section, entry, out _, out var source, out var units))
            return 0;

        var amount = source.Amount ?? int.MaxValue;
        if (amount == int.MaxValue)
            return int.MaxValue;

        // A partly filled last stack cannot satisfy an order for a complete product or package.
        var prototype = _prototypes.Index(source.Id);
        if (prototype.TryGetComponent<StackComponent>(out var stack, _compFactory) &&
            vendor.Comp.PartialProductStacks.GetValueOrDefault(stack.StackTypeId) > 0)
            amount--;
        return Math.Max(0, amount) / units;
    }

    public bool TryGetRequisitionSource(Entity<CMAutomatedVendorComponent> vendor,
        CMVendorSection section, CMVendorEntry entry,
        out CMVendorSection sourceSection, out CMVendorEntry source, out int units)
    {
        sourceSection = section;
        source = entry;
        units = 1;
        if (!IsRequisitionProduct(section, entry))
            return false;

        if (entry.Box is not { } stockId)
            return true;

        foreach (var candidateSection in vendor.Comp.Sections)
        {
            foreach (var candidate in candidateSection.Entries)
            {
                if (candidate.Id != stockId)
                    continue;
                if (candidate.Box != null || !IsRequisitionProduct(candidateSection, candidate))
                    return false;

                sourceSection = candidateSection;
                source = candidate;
                units = GetBoxRemoveAmount(entry);
                return true;
            }
        }
        return false;
    }

    private bool IsRequisitionProduct(CMVendorSection section, CMVendorEntry entry)
    {
        if (!IsRequisitionEntry(section, entry) ||
            !_prototypes.TryIndex(entry.Id, out var prototype) ||
            !prototype.HasComponent<ItemComponent>(_compFactory) ||
            prototype.HasComponent<CMVendorBundleComponent>(_compFactory) ||
            prototype.HasComponent<CMVendorMapToSquadComponent>(_compFactory) ||
            prototype.HasComponent<CMChangeUserOnVendComponent>(_compFactory))
            return false;
        return true;
    }

    public bool TryIssueRequisition(Entity<CMAutomatedVendorComponent> vendor, EntityUid actor, int section, int entry)
    {
        if (_net.IsClient || !_accessReader.IsAllowed(actor, vendor) || !_requisitionPower.IsPowered(vendor) ||
            section < 0 || section >= vendor.Comp.Sections.Count ||
            entry < 0 || entry >= vendor.Comp.Sections[section].Entries.Count)
            return false;

        var selected = vendor.Comp.Sections[section].Entries[entry];
        if (GetRequisitionAmount(vendor, vendor.Comp.Sections[section], selected) <= 0)
            return false;

        var attempt = new ActivatableUIOpenAttemptEvent(actor);
        RaiseLocalEvent(vendor.Owner, attempt);
        if (attempt.Cancelled)
            return false;

        if (selected.Box != null)
        {
            var available = GetRequisitionAmount(vendor, vendor.Comp.Sections[section], selected);
            if (available <= 0)
                return false;
            selected.Amount = available == int.MaxValue ? null : available;
        }

        var message = new CMVendorVendBuiMsg(section, entry, new()) { Actor = actor };
        VendFromMenu(vendor, ref message, autoGrab: false);
        return true;
    }

}
