using System.Numerics;
using System.Reflection.Metadata;
using Content.Shared._RMC14.EyeProtection;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client._RMC14.EyeProtection;

public sealed class EyeProtectionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
/*
    private readonly ContainerSystem _container;
    private readonly TransformSystem _transform;
*/
    private readonly ShaderInstance _eyeProtShader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    //private readonly List<NightVisionRenderEntry> _entries = new();

    public EyeProtectionOverlay()
    {
        IoCManager.InjectDependencies(this);
/*
        _container = _entity.System<ContainerSystem>();
        _transform = _entity.System<TransformSystem>();
*/
        _eyeProtShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out RMCEyeProtectionComponent? eyeProt) ||
            !eyeProt.Enabled)
        {
            return;
        }

        var viewport = args.WorldAABB;
        var handle = args.WorldHandle;
        var distance = args.ViewportBounds.Width;

        float level = 0.5f;

        float outerMaxLevel = 2.0f * distance;
        float outerMinLevel = 0.8f * distance;
        float innerMaxLevel = 0.6f * distance;
        float innerMinLevel = 0.2f * distance;

        var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
        var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

        _eyeProtShader.SetParameter("time", 0f);
        _eyeProtShader.SetParameter("color", new Vector3(0f,0f,0f));
        _eyeProtShader.SetParameter("darknessAlphaOuter", 0.8f);

        _eyeProtShader.SetParameter("outerCircleRadius", outerRadius);
        _eyeProtShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
        _eyeProtShader.SetParameter("innerCircleRadius", innerRadius);
        _eyeProtShader.SetParameter("innerCircleMaxRadius", innerRadius + 0.02f * distance);
        handle.UseShader(_eyeProtShader);
        handle.DrawRect(viewport, Color.White);

        handle.UseShader(null);
    }


}
