using Content.Client.Graphics;

namespace Content.Client._RMC14.Graphics;

public static class RMCPostShaderIds
{
    public const string Aura = "rmc-aura";
    public const string Invisible = "rmc-invisible";
    public const string GhostAlpha = "rmc-ghost-alpha";

    public static readonly string[] BeforeOpacity =
    {
        ContentPostShaderIds.Stealth,
        ContentPostShaderIds.FloorOcclusion,
        ContentPostShaderIds.Holopad,
        ContentPostShaderIds.InteractionOutline,
        ContentPostShaderIds.TargetOutline,
        ContentPostShaderIds.DragDropOutline,
        Aura,
    };
}
