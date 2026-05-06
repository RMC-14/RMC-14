using System.Linq;

// ReSharper disable CheckNamespace
namespace Content.Shared.Mobs.Components;

public sealed partial class MobThresholdsComponent : IComponentDebug
{
    public string GetDebugString()
    {
        var thresholdStrings = Thresholds.Select(item => $"{item.Key}: {item.Value}");
        var alertStrings = StateAlertDict.OrderBy(item => (int)item.Key).Select(item => $"{item.Key}: {item.Value}");
        return $"""
            TriggersAlerts: {TriggersAlerts}
            Thresholds:
              {string.Join("\r\n  ", thresholdStrings)}
            StateAlertDict:
              {string.Join("\r\n  ", alertStrings)}
            """;
    }
}
