using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Deploy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCDeployableComponent : Component
{
    // Время ду афтера (секунды)
    [DataField, AutoNetworkedField]
    public float DeployTime = 10f;

    // Фигура области развертывания (например, PhysShapeAabb)
    [DataField(required: true), AutoNetworkedField]
    public PhysShapeAabb DeployArea = new();

    // Набор объектов для спавна
    [DataField(required: true), AutoNetworkedField]
    public List<RMCDeploySetup> DeploySetups = new();

    // Пропускать ли проверку занятости области
    [DataField, AutoNetworkedField]
    public bool AreaBlockedCheck = false;

    // Проверять ли поверхность планеты и космос (как в RangefinderSystem и TileNotBlocked)
    [DataField, AutoNetworkedField]
    public bool FailIfNotSurface = true;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RMCDeploySetup
{
    // Прототип сущности для спавна
    [DataField(required: true)] public EntProtoId Prototype;

    // Смещение относительно центра области
    [DataField] public Vector2 Offset = Vector2.Zero;

    // Угол поворота (градусы)
    [DataField] public float Angle = 0f;

    // Закреплять ли по центру ближайшего тайла
    [DataField] public bool Anchor = true;
}
