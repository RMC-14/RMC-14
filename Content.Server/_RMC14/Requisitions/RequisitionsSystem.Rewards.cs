using System.Linq;
using Content.Server.Labels.Components;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Requisitions
{
    public sealed partial class RequisitionsSystem
    {
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _slots = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private void PrintInvoice(EntityUid requisitionOrder, EntityCoordinates coordinates, string paperwork)
        {
            // Create a sheet of paper to write the order details on
            // Order information sprinkled with flavor text
            var printedPaper = EntityManager.SpawnEntity(paperwork, coordinates);

            if (TryComp<PaperComponent>(printedPaper, out var paper))
            {
                // Generate random serial and lot number
                int serialNum = _robustRandom.Next(10000, 999999);
                int lotNum = _robustRandom.Next(10, 99);

                string orderName = MetaData(requisitionOrder).EntityName;
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
        }

        private int SubmitInvoices(EntityUid uid)
        {
            int compoundRewards = 0;

            if (!TryComp<ContainerManagerComponent>(uid, out var container))
                return compoundRewards;

            // It iterate everything inside the crate, including labelSlot
            foreach (var containerValues in container.Containers.Values)
            {
                foreach (var content in containerValues.ContainedEntities)
                {
                    if (TryComp(content, out RequisitionsInvoiceComponent? containerInvoice)
                        && TryComp(content, out PaperComponent? containerPaper)
                        && containerPaper.StampState == containerInvoice.RequiredStamp)
                    {
                        compoundRewards += containerInvoice.Reward;
                    }
                }
            }

            return compoundRewards;
        }
    }
}
