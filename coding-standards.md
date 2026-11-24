# Coding Standards
## .editorconfig

Make sure that your editor is correctly reading the project's .editorconfig file at the root of the project.

This will automatically format and give warnings for code that needs fixing.

## Variables

* Variables should be implicitly typed with `var` where possible.

## Components

* Components should be defined in Content.Shared as much as possible, with `RegisterComponent`, `NetworkedComponent`, `AutoGenerateComponentState`, and `Access` attributes.
  * Typing `nscomp` above the component's class definition automatically fills these in for you.
* Fields on the component should have the `DataField` and `AutoNetworkedField` attributes where relevant. `DataField` makes the data be read from and written to YAML, and `AutoNetworkedField` synchronizes changes over the network to the client.
  * Typing `nfield` above a field automatically fills these in for you.
* `DataField` attributes should not include a custom field name, it will automatically use the field name's camel-cased. If the names don't match, they should be made to match instead of specifying the data filed name explicitly. For example:

  ```csharp
  [DataField] public string Field;
  ```

  Instead of:

  ```csharp
  [DataField("field")] public string Field;
  ```

  Both work the same way, but the first is preferred in all cases.

## Entity Systems

* Systems should be defined in `Content.Shared` as much as possible.
  * A system should only be split up into client and server components if there are methods that otherwise can't be moved to shared, such as database methods.
  * If you just need a system to handle UI code, create a new one in `Content.Client`, but leave the one in `Content.Shared` as sealed, this avoids having an empty system in `Content.Server`. For example, ThingSystem in Shared, and ThingUISystem in Client.
* Event subscriptions in `Initialize` should use the overload that uses an `Entity<T>` argument, not either of the two that use `EntityUid`.
* Event subscriptions should use method names that start with "On".
* Large amounts of entities should not be queried every tick in an `Update` method. Event subscriptions and components that mark entities to be processed next update should be used instead.
* Values used in system code should be able to be defined through a component's YAML, or at least a CVar defined in RMCCVars.
  * Prototype IDs, if not defined in a component, should be stored in a static readonly `ProtoId<T>` field, so that it can be validated by the YAML linter.
* Dependencies to other systems and managers should be listed at the top of the file, together and in alphabetical order. The field name of system dependencies should not include the word "System" in it.
  * For example: `_damageable` instead of `_damageableSystem`.
* Do not make a dependency for `IEntityManager` in systems, it already has one with the field name `EntMan`.
* Proxy methods should be used over `IEntityManager` methods, for example TryComp over `EntMan.TryGetComponent`.
* Calls to `EntityLookupSystem` methods should use the overloads that allow you to pass in a `HashSet`, instead of creating and returning a new one. This `HashSet` should be stored in a `private readonly` field on the system that calls the method.

## Events

* Events should be record structs where possible, with a `ByRefEvent` attribute added above the struct event definition.
* Any events that need to inherit from other types need to be classes instead, such as `EntityEventArgs` sent over the network.
* `HandledEntityEventArgs` does not need to be inherited, a boolean field `Handled` should be added to the record struct event instead.
  * This also applies to `CancellableEventArgs`, adding a boolean field `Cancelled` instead.
* Events should be readonly when no data on them is supposed to be changed by event handlers.

## Prototypes

* New prototype types (kinds) should not be created. Entity prototypes with components should be used instead. This automatically enables localization of names and descriptions, for example.
* The prototype can be resolved by ID through `IPrototypeManager`, and the component on the prototype obtained through `TryComp` on the `EntityPrototype`, passing in an instance of `IComponentFactory`.
  * The data on component instances tied to an `EntityPrototype` should never be changed in code.
* Singleton entities can also be created for these entities, and events can be raised on them (see surgery code for an example).
* Prototype data should never be changed in code, as those changes are not synced between client and server, and will persist between round restarts.
  * The exception to this is prototype hot reloading, which updates prototypes when the `loadprototype` admin command is used, or when a YAML file is changed in local development.
* New fields in upstream prototypes should be added in a new partial class defined under an _RMC14 folder. To do this, change the namespace of the file to be equal to that of the upstream file, and suppress the warning that the namespace is incorrect. Example below:

  ```csharp
  // ReSharper disable CheckNamespace

  namespace Content.Shared.Humanoid.Markings;

  public sealed partial class MarkingPrototype
  {
      [DataField] public readonly Vector2 Offset;
  }
  ```

## Database

* Upstream tables shouldn't be modified, new tables should be created instead, with a foreign key to the upstream table that it associates with. For example, to add relational data to the Player table:

```csharp
[Table("rmc_player_data")]
[PrimaryKey(nameof(PlayerId), nameof(SomeId))]
public sealed class RMCPlayerData
{
    [Key]
    [ForeignKey("Player")]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    [Key]
    public int SomeId { get; set; } = null!;
}
```

* Primary keys should be defined in the order in which they will be queried. For example, in the code above, lookups for `PlayerId` or `PlayerId + SomeId` will be fast, but just `SomeId` will be slow.
  * If you need to do lookups in a different order, an index for them should be added separately.

## User Interface

* UI elements should be defined and created through `.xaml` files as much as possible, not in C# code.
* Bound user interface states should not be used, add the data to components instead. BUI code can access any component on the Owner entity (the one that the UI was opened on) directly.
  * If you need to refresh the UI when the component's data is changed, handle the `AfterAutoHandleStateEvent` that is raised directed at that component.
    * To do this, make sure that the `AutoGenerateComponentState` has `raiseAfterAutoHandleState` set to true (`AutoGenerateComponentState(true)`).
    * Ensure that you wrap the contents of the method handling the event in a try-catch block that logs the error, as any errors thrown within it will cause the client's screen to flash and refresh repeatedly otherwise. Errors in UI code are common.
* Most business code should be on the BUI, while UI elements (`.xaml.cs` files) should only contain methods that help with modifying the appearance of the element.

## Localization

* New localization IDs should be defined under the _RMC14 directory, not in existing upstream `.ftl` files.
* You are not required to localize your code for RMC14, but you may if you want to.
* You are free to make a PR localizing existing RMC14 code, however be aware that it may not be reviewed over other pending PRs until time allows, specially if the PR is very big, as the game is still in active development. Make sure to test that your changes work properly in-game.

## Upstream code changes

* New code and yml should be put in files inside `_RMC14` folders as much as possible, using events and making new ones where needed to make this possible.
  * For example, if you wanted to change how zombies work, you should make an `RMCZombieSystem` that handles the `EntityZombifiedEvent` event, running your code there, instead of modifying the upstream `ZombifyEntity` method.
* Where this is not possible, the code should have `// RMC14` markers above and below every block of code that was changed. For example:

  ```csharp
  // RMC14
  your new code here
  new code line 2
  new line 3
  // RMC14
  ```
* Changes to the upstream code should call RMC-specific methods wherever possible to minimize the number of lines that need to be changed. This makes merge conflicts easier to process in the future.

## TODOs

* TODOs should be marked with `// TODO RMC14`, not just `// TODO`, which allows us to later see TODOs that are specific to RMC14.

## Content removals

* Upstream files and file contents should not be removed unless completely necessary.
  * For example, if we don't want access to a specific weapon, that weapon should be commented out from any sources that give you access to it, instead of deleting the weapon's YAML or the file that it is in.
