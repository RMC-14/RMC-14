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

    // Фигура области развертывания. Началом координат для фигуры является центр ближайшего тайла, на котором стоит игрок.
    [DataField(required: true), AutoNetworkedField]
    public PhysShapeAabb DeployArea = new();

    // Набор объектов для спавна
    [DataField(required: true), AutoNetworkedField]
    public List<RMCDeploySetup> DeploySetups = new();

    // Пропускать ли проверку занятости области
    [DataField, AutoNetworkedField]
    public bool AreaBlockedCheck = false;

    // Проверять ли поверхность планеты и космос
    [DataField, AutoNetworkedField]
    public bool FailIfNotSurface = true;

    // Tracks indices of setups whose entities were destroyed and should not be redeployed.  Not for YAML! Only for runtime use.
    [DataField, AutoNetworkedField]
    public HashSet<int> NonRedeployableSetups = new();

    void ISerializationHooks.AfterDeserialization()
    {
        if (DeploySetups == null || DeploySetups.Count == 0)
            return;
        int parentalIndex = -1;
        for (int i = 0; i < DeploySetups.Count; i++)
        {
            if (DeploySetups[i].ParentalSetup)
            {
                parentalIndex = i;
                break;
            }
        }
        if (parentalIndex == -1)
        {
            // Нет ни одного ParentalSetup — делаем первый
            DeploySetups[0].ParentalSetup = true;
            DeploySetups[0].StorageOriginalEntity = true;
        }
        else
        {
            // Первый ParentalSetup получает StorageOriginalEntity
            DeploySetups[parentalIndex].StorageOriginalEntity = true;
        }

        if (NonRedeployableSetups.Count > 0) // YAML protection
            NonRedeployableSetups = new();
    }
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RMCDeploySetup : ISerializationHooks
{
    // Прототип сущности для спавна
    [DataField(required: true)] public EntProtoId Prototype;

    // Помечает сетап как условный "родитель" для всех сетапов, не помеченных как ParentalSetup =true.
    // Необходимо для реализации свертывания а также реагирования на удаление сущностей из сетапов, помеченных как ParentalSetup =true (если включено).
    // В сущности первого по списку DeploySetups сетапе, помеченного как ParentalSetup (или вообще в первом в списке, если нет ни одного ParentalSetup) будет храниться сущность,
    // которая развернула все сетапы, до сворачивания или уничтожения.
    // Если ни один сетап не помечен как ParentalSetup, по умолчанию первый в списке DeploySetups будет считаться как ParentalSetup.
    [DataField]
    public bool ParentalSetup
    {
        get => _parentalSetup;
        set
        {
            _parentalSetup = value;
            if (value && !ReactiveSetup) // runtime protection (will also trigger if the deserialization of this field occurs later than the ReactiveSetup field)
                ReactiveSetup = true;
        }
    }
    private bool _parentalSetup = false;

    // Если true, то сущность, развернуютая с помощью  этого сетапа, будет реагировать на удаление всех сущностей,
    // развернутых из сетапов, помеченных как ParentalSetup и будет также уничтожена.
    //
    // Логика обязывает пометить  ParentalSetup также как ReactiveSetup. Если вы указали в yaml ParentalSetup как не ReactiveSetup, система все равно будет считать его таковым.
    // Это позволит сворачивать, взаимодействуя с одной из таких сущностей, при этом все эти сущности будут реагиорвать на удаление друг-друга.
    // Таким образом это преодотвращает ошибку, когда сущность, развернутая из ParentalSetup, и  хранящая в себе сущность, с помощью которой все сетапы были развернуты,
    // была удалена, а остальным сущностям, развернутым из ParentalSetup, уже не во что сворачивать сетапы.
    [DataField] public bool ReactiveSetup = true;

    // If true, this setup will never be redeployed or collapsed
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
        if (ParentalSetup && !ReactiveSetup) // YAML protection
            ReactiveSetup = true;

        if (StorageOriginalEntity) // YAML protection
            StorageOriginalEntity = false;

    }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCDeploySystem), Other = AccessPermissions.Read)]
public sealed partial class RMCDeployedEntityComponent : Component, ISerializationHooks
{
    // The original entity that initiated the deploy
    [DataField, AutoNetworkedField]
    public EntityUid OriginalEntity;

    // The index of the setup in DeploySetups that spawned this entity
    [DataField, AutoNetworkedField]
    public int SetupIndex;

    // Флаг для защиты от повторной обработки при удалении
    [DataField, AutoNetworkedField]
    public bool InShutdown = false;

    void ISerializationHooks.AfterDeserialization()
    {
        if (OriginalEntity != EntityUid.Invalid || SetupIndex != 0 || InShutdown) // YAML protection
        {
            OriginalEntity = EntityUid.Invalid;
            SetupIndex = 0;
            InShutdown = false;
        }
    }
}
