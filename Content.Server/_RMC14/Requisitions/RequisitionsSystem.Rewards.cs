using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Requisitions
{
    public sealed partial class RequisitionsSystem
    {
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _slots = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;

        private void PrintInvoice(EntityUid requisitionOrder, EntityCoordinates coordinates, string paperwork)
        {
            // Create a sheet of paper to write the order details on
            // Order information sprinkled with flavor text
            var printedPaper = EntityManager.SpawnEntity(paperwork, coordinates);

            if (!TryComp<PaperComponent>(printedPaper, out var paper))
                return;

            // Generate random serial and lot number
            var serialNum = _random.Next(10000, 999999);
            var lotNum = _random.Next(10, 99);

            var orderName = MetaData(requisitionOrder).EntityName;
            uint weight = 10;
            var contentList = new FormattedMessage();

            // if its a crate
            if (TryComp(requisitionOrder, out ContainerManagerComponent? containerComp))
            {
                // list order content
                foreach (var container in containerComp.Containers.Values)
                {
                    var entityIndex = 0;
                    var contentCount = container.ContainedEntities.Count;

                    // Filter and count each entity type
                    var content = container.ContainedEntities.GroupBy(
                        item => MetaData(item).EntityName,
                        item => item,
                        (itemName, itemGroup) => new
                        {
                            Key = itemName,
                            Name = itemName,
                            Count = itemGroup.Count()
                        });

                    if (contentCount < 1)
                        continue;

                    contentList.PushNewline();

                    foreach (var entity in content)
                    {
                        // If its just the same name as the like the one on crate, ignore it.
                        if (entity.Name == orderName)
                            continue;

                        weight += 10;
                        contentList.AddMarkupOrThrow($"{Loc.GetString("requisition-paper-print-content",
                            ("count", entity.Count),
                            ("item", entity.Name.ToUpper()))}");

                        if (entityIndex == contentCount)
                            continue;

                        contentList.PushNewline();
                        entityIndex++;
                    }
                }
            }

            _metaSystem.SetEntityName(printedPaper, Loc.GetString(
                "requisition-paper-print-name", ("name", orderName)));

            _paperSystem.SetContent((printedPaper, paper), Loc.GetString(
                "requisition-paper-print-manifest",
                ("containerName", orderName.ToUpper()),
                ("content", contentList.ToMarkup()),
                ("weight", weight),
                ("lot", lotNum),
                ("serialNumber", $"{serialNum:000000}")));

            // attempt to attach the label to the item
            if (TryComp<PaperLabelComponent>(requisitionOrder, out var label))
            {
                _slots.TryInsert(requisitionOrder, label.LabelSlot, printedPaper, null);
            }
        }

        private bool IsInvoice(Entity<PaperComponent?> ent, [NotNullWhen(true)] out RequisitionsInvoiceComponent? invoice)
        {
            invoice = null;
            if (!Resolve(ent, ref ent.Comp, false))
                return false;

            if (!TryComp(ent.Owner, out invoice))
                return false;

            return ent.Comp.StampState == invoice.RequiredStamp;
        }

        private int SubmitInvoices(EntityUid uid)
        {
            var compoundRewards = 0;
            if (IsInvoice(uid, out var invoice))
                compoundRewards += invoice.Reward;

            if (!TryComp<ContainerManagerComponent>(uid, out var container))
                return compoundRewards;

            // It iterates everything inside the crate, including labelSlot
            foreach (var containerValues in container.Containers.Values)
            {
                foreach (var content in containerValues.ContainedEntities)
                {
                    if (IsInvoice(content, out invoice))
                        compoundRewards += invoice.Reward;
                }
            }

            return compoundRewards;
        }
    }
}
