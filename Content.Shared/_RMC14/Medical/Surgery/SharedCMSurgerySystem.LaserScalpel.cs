using Content.Shared._RMC14.Medical.Surgery.Steps;
using Content.Shared._RMC14.Medical.Surgery.Tools;

namespace Content.Shared._RMC14.Medical.Surgery;

public abstract partial class SharedCMSurgerySystem
{
    private static readonly Dictionary<string, int> RequiredScalpelTierByHeart = new()
    {
        ["RMCOrganXenoHeartT3"] = 3,
        ["RMCOrganXenoHeartTQ"] = 3,
        ["RMCOrganXenoHeartKing"] = 3,
    };

    private void InitializeLaserScalpelGate()
    {
        SubscribeLocalEvent<RMCSurgeryRequiresLaserScalpelComponent, CMSurgeryCanPerformStepEvent>(OnRequiresLaserScalpel);
    }

    private void OnRequiresLaserScalpel(Entity<RMCSurgeryRequiresLaserScalpelComponent> ent, ref CMSurgeryCanPerformStepEvent args)
    {
        if (args.Invalid != StepInvalidReason.None)
            return;

        if (!TryComp<RMCSurgeryXenoHeartComponent>(args.Body, out var heart) ||
            !RequiredScalpelTierByHeart.TryGetValue(heart.Item.Id, out var minTier))
        {
            return;
        }

        foreach (var tool in args.Tools)
        {
            if (TryComp(tool, out CMLaserScalpelTierComponent? tier) && tier.Tier >= minTier)
                return;
        }

        args.Invalid = StepInvalidReason.MissingTool;
        args.Popup = "This specimen's tissue requires a more advanced laser scalpel to sever safely!";
    }
}
