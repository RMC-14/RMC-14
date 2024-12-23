using Content.Shared._RMC14.SightRestriction;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.SightRestriction;

public sealed class SightRestrictionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _sightRestrictShader;

    // Default view distance from top to middle of screen in tiles
    private readonly float _maxTilesHeight = 8.5f;

    private Vector3 _color = new Vector3(0f, 0f, 0f);
    private float _outerRadius;
    private float _innerRadius;
    private float _darknessAlphaOuter = 1.0f;
    private float _darknessAlphaInner = 0.0f;

    public SightRestrictionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sightRestrictShader = _prototypeManager.Index<ShaderPrototype>("GradientCircleMask").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var eyeComp))
            return;

        if (args.Viewport.Eye != eyeComp.Eye)
            return;

        if (!_entityManager.TryGetComponent<SightRestrictionComponent>(playerEntity, out var sightRestrict))
            return;

        var restrictions = sightRestrict.Restrictions;
        if (restrictions.Count == 0)
            return;

        // TODO: find strongest restriction and apply it
        var appliedRestrict = new SightRestrictionDefinition();
        foreach (var currentRestrict in restrictions.Values)
        {
            if (currentRestrict > appliedRestrict)
                appliedRestrict = currentRestrict;
        }

        var handle = args.WorldHandle;
        var viewport = args.WorldAABB;
        var viewportHeight = args.ViewportBounds.Height;

        // Actual height of viewport in tiles, accounting for zoom
        var actualTilesHeight = _maxTilesHeight * eyeComp.Zoom.X;

        var outerRadiusRatio = (_maxTilesHeight - appliedRestrict.ImpairFull.Float()) / actualTilesHeight / 2;
        var innerRadiusRatio = (_maxTilesHeight - appliedRestrict.ImpairFull.Float() - appliedRestrict.ImpairPartial.Float()) / actualTilesHeight / 2;

        _innerRadius = innerRadiusRatio * viewportHeight;
        _outerRadius = outerRadiusRatio * viewportHeight;
        _darknessAlphaInner = appliedRestrict.AlphaInner.Float();
        _darknessAlphaOuter = appliedRestrict.AlphaOuter.Float();

        // Shouldn't be time-variant
        _sightRestrictShader.SetParameter("time", 0.0f);
        // Outside area should be black
        _sightRestrictShader.SetParameter("color", _color);
        _sightRestrictShader.SetParameter("darknessAlphaInner", _darknessAlphaInner);
        _sightRestrictShader.SetParameter("darknessAlphaOuter", _darknessAlphaOuter);
        // Radius should stay constant
        _sightRestrictShader.SetParameter("outerCircleRadius", _outerRadius);
        _sightRestrictShader.SetParameter("outerCircleMaxRadius", _outerRadius);
        _sightRestrictShader.SetParameter("innerCircleRadius", _innerRadius);
        _sightRestrictShader.SetParameter("innerCircleMaxRadius", _innerRadius);

        handle.UseShader(_sightRestrictShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
