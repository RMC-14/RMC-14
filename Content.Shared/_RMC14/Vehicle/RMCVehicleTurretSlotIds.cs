using System;

namespace Content.Shared._RMC14.Vehicle;

public static class RMCVehicleTurretSlotIds
{
    public const string Separator = "::";

    public static string Compose(string parentSlotId, string childSlotId)
    {
        return $"{parentSlotId}{Separator}{childSlotId}";
    }

    public static bool TryParse(string slotId, out string parentSlotId, out string childSlotId)
    {
        parentSlotId = string.Empty;
        childSlotId = string.Empty;

        if (string.IsNullOrWhiteSpace(slotId))
            return false;

        var index = slotId.IndexOf(Separator, StringComparison.Ordinal);
        if (index <= 0 || index >= slotId.Length - Separator.Length)
            return false;

        parentSlotId = slotId[..index];
        childSlotId = slotId[(index + Separator.Length)..];

        return !string.IsNullOrWhiteSpace(parentSlotId) && !string.IsNullOrWhiteSpace(childSlotId);
    }
}
