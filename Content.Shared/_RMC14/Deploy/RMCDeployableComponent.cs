using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Deploy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCDeploySystem), Other = AccessPermissions.Read)]
public sealed partial class RMCDeployableComponent : Component, ISerializationHooks
{

    // Время ду афтера (секунды)
    [DataField, AutoNetworkedField]
    public float DeployTime = 10f;

    // Время ду афтера (секунды)
    [DataField, AutoNetworkedField]
    public float CollapseTime = 10f;

    // Фигура области развертывания. Началом координат для фигуры является центр ближайшего тайла, на котором стоит игрок.
    [DataField(required: true), AutoNetworkedField]
    public PhysShapeAabb DeployArea = new();

    /// <summary>
    /// Набор объектов для спавна
    /// </summary>
    /// <remarks>
    /// В сущности первого по списку DeploySetups сетапе, помеченного как ReactiveParental (или вообще в первом в списке, если нет ни одного ReactiveParental) будет храниться сущность,
    /// которая развернула все сетапы, до сворачивания или уничтожения.
    /// Если ни один сетап не помечен как ReactiveParental, по умолчанию первый в списке DeploySetups будет считаться как ReactiveParental.
    /// </remarks>
    [DataField(required: true), AutoNetworkedField]
    public List<RMCDeploySetup> DeploySetups = new();

    // Проводить ли проверку блокировки области
    [DataField, AutoNetworkedField]
    public bool AreaBlockedCheck = false;

    // Проверять ли поверхность планеты и космос
    [DataField, AutoNetworkedField]
    public bool FailIfNotSurface = true;

    // ID прототипа инструмента, который используется для сворачивания. Не указывайте, если не хотите, чтобы была возможность сворачивать.
    [DataField, AutoNetworkedField]
    public EntProtoId? CollapseToolPrototype;


    void ISerializationHooks.AfterDeserialization()
    {
        if (DeploySetups == null || DeploySetups.Count == 0)
            return;
        int parentalIndex = -1;
        for (int i = 0; i < DeploySetups.Count; i++)
        {
            if (DeploySetups[i].Mode == RMCDeploySetupMode.ReactiveParental)
            {
                parentalIndex = i;
                break;
            }
        }
        if (parentalIndex == -1)
        {
            // Нет ни одного ReactiveParentalSetup — делаем первый
            DeploySetups[0].Mode = RMCDeploySetupMode.ReactiveParental;
            DeploySetups[0].StorageOriginalEntity = true;
        }
        else
        {
            // Первый ReactiveParentalSetup получает StorageOriginalEntity
            DeploySetups[parentalIndex].StorageOriginalEntity = true;
        }
    }
}


[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RMCDeploySetup : ISerializationHooks
{
    // Прототип сущности для спавна
    [DataField(required: true)] public EntProtoId Prototype;

    /// <summary>
    /// Мод сетапа, определяющий реакцию развернутой с помощью сетапа сущности на различные события
    /// </summary>
    [DataField]
    public RMCDeploySetupMode Mode = RMCDeploySetupMode.Default;

    // If true, this setup will never be redeployed and collapsed
    [DataField] public bool NeverRedeployableSetup = false;

    // Служебный флаг для определение в сущности из какого сетапа будет хранится оригинальная сущность до сворачивания или уничтожения.
    //  Not for YAML! Only for runtime use.
    [DataField] public bool StorageOriginalEntity = false;

    // Смещение относительно центра области развертывания
    [DataField] public Vector2 Offset = Vector2.Zero;

    // Угол поворота (градусы)
    [DataField] public float Angle = 0f;

    // Закреплять ли по центру ближайшего тайла
    [DataField] public bool Anchor = true;


    void ISerializationHooks.AfterDeserialization()
    {
        if (StorageOriginalEntity) // YAML protection
            StorageOriginalEntity = false;

    }
}


[Serializable, NetSerializable]
public enum RMCDeploySetupMode
{
    /// <summary>
    /// Сущность, развернутая с помощью  такого сетапа, не реагирует на удаление сущущностей, развернутых из ReactiveParental
    /// и не могут быть как источником сворачивания, так и хранилищем оригинальной сущности.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Сущность, развернутая с помощью  такого сетапа, будет реагировать на удаление всех сущностей,
    /// развернутых из сетапов, помеченных как ReactiveParental и будет также удалена.
    /// </summary>
    Reactive = 1,

    /// <summary>
    /// Помечает сетап как один из условных "родителей" для всех сетапов, не помеченных как ReactiveParental.
    /// Необходим для реализации свертывания а также реагирования на удаление сущностей из сетапов, помеченных как ReactiveParental.
    /// </summary>
    ReactiveParental = 2
}
