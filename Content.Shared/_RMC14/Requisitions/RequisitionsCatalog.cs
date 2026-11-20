using Content.Shared.Containers;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Requisitions;

public static class RequisitionsCatalog
{
    public static string GetContents(RequisitionsEntry entry, IPrototypeManager prototypes, IComponentFactory factory)
    {
        var lines = new List<string>();
        var quantities = new Dictionary<EntProtoId, int>();
        foreach (var entity in entry.Entities)
        {
            quantities[entity] = quantities.GetValueOrDefault(entity) + 1;
        }

        if (prototypes.TryIndex(entry.Crate, out var crate))
        {
            if (crate.TryGetComponent<StorageFillComponent>(out var storage, factory))
            {
                foreach (var item in storage.Contents)
                {
                    if (item.PrototypeId is not { } id || !prototypes.TryIndex(id, out var prototype))
                        continue;

                    var amount = item.MaxAmount > item.Amount
                        ? $"{item.Amount}–{item.MaxAmount}"
                        : item.Amount.ToString();
                    var line = Loc.GetString("rmc-requisitions-contents-item",
                        ("amount", amount), ("name", prototype.Name));
                    if (item.SpawnProbability < 1 || item.GroupId != null)
                        line = Loc.GetString("rmc-requisitions-contents-random", ("item", line));
                    lines.Add(line);
                }
            }

            if (crate.TryGetComponent<ContainerFillComponent>(out var containers, factory))
            {
                foreach (var contents in containers.Containers.Values)
                {
                    foreach (var id in contents)
                    {
                        EntProtoId prototype = id;
                        quantities[prototype] = quantities.GetValueOrDefault(prototype) + 1;
                    }
                }
            }
        }

        foreach (var (id, amount) in quantities)
        {
            if (prototypes.TryIndex(id, out var prototype))
            {
                lines.Add(Loc.GetString("rmc-requisitions-contents-item",
                    ("amount", amount), ("name", prototype.Name)));
            }
        }

        return lines.Count == 0 ? Loc.GetString("rmc-requisitions-contents-unspecified") : string.Join("\n", lines);
    }
}
