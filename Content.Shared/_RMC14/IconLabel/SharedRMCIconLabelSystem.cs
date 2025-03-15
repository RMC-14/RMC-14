using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Labels.Components;
using Content.Shared.SubFloor;

namespace Content.Shared._RMC14.IconLabel;

public abstract class SharedRMCIconLabelSystem : EntitySystem
{
    public static string ToLabelShortForm(string label, int labelMaxSize)
    {
        // Get the first [0 to labelMaxSize) characters of [label]
        // Ex. label="Bicaridine 2 OD", labelMaxSize=3 -> "Bic"
        string shortened = label.Substring(0, Math.Min(label.Length, labelMaxSize));

        // The last (typically 3rd) character of the label may only be a +, or it will be truncated
        if (shortened.Length > 2 && shortened[shortened.Length - 1] != '+')
            shortened = shortened.Substring(0, shortened.Length - 1);

        return shortened;
    }

    // For pill bottles that have a custom label applied to make an icon label appear
    // since previous icon labels depend on localized strings in a yaml file
    public bool GetLabelForIcon(EntityUid entity, out string label)
    {
        if (TryComp(entity, out LabelComponent? labelComponent))
        {
            label = labelComponent.CurrentLabel ?? string.Empty;
            return true;
        }

        label = string.Empty;
        return false;
    }
}
