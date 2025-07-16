using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization;
using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Deploy;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCDeployableComponent : Component
{
    // Время ду афтера (секунды)
    [DataField]
    public float DeployTime = 10f;

    // Фигура области развертывания (например, PhysShapeAabb)
    [DataField(required: true)]
    public PhysShapeAabb DeployArea = new();

    // Набор объектов для спавна
    [DataField(required: true)]
    public List<RMCDeploySetup> DeploySetups = new();

    // Пропускать ли проверку занятости области
    [DataField]
    public bool SkipAreaOccupiedCheck = false;

    // Проверять ли мобов в зоне
    [DataField]
    public bool CheckMobsInArea = true;

    // Проверять ли поверхность планеты и космос (как в RangefinderSystem и TileNotBlocked)
    [DataField]
    public bool FailIfNotSurface = true;
}

[DataDefinition]
public sealed partial class RMCDeploySetup
{
    // Прототип сущности для спавна
    [DataField(required: true)] public EntProtoId Prototype;
    // Смещение относительно центра области
    [DataField] public Vector2 Offset = Vector2.Zero;
    // Угол поворота (градусы)
    [DataField] public float Angle = 0f;
    // Закреплять ли по центру тайла (по умолчанию true)
    [DataField] public bool AnchorToTileCenter = true;
}
