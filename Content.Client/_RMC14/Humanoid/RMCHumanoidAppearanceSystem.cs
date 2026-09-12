using Content.Shared._RMC14.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client.Humanoid;

public sealed partial class HumanoidAppearanceSystem
{
    public void RefreshAppearance(EntityUid entity, IRMCHumanoidAppearance humanoid, SpriteComponent sprite)
    {
        UpdateSprite(entity, humanoid, sprite);
    }
}
