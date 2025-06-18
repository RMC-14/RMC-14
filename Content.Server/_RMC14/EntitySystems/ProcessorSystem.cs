using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Linq;
using Content.Server._RMC14.Kitchen;
using Content.Server.Jittering;
using Content.Shared._RMC14.Kitchen;
using Content.Shared.Jittering;
using Content.Shared.Power;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ProcessorSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
        [Dependency] private readonly JitteringSystem _jitter = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActiveProcessorComponent, ComponentStartup>(OnActiveProcessorStart);
            SubscribeLocalEvent<ActiveProcessorComponent, ComponentRemove>(OnActiveProcessorRemove);
            SubscribeLocalEvent<ProcessorComponent, ComponentStartup>((uid, _, _) => UpdateUiState(uid));
            SubscribeLocalEvent((EntityUid uid, ProcessorComponent _, ref PowerChangedEvent _) => UpdateUiState(uid));
            SubscribeLocalEvent<ProcessorComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeLocalEvent<ProcessorComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<ProcessorComponent, EntRemovedFromContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<ProcessorComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);

            SubscribeLocalEvent<ProcessorComponent, ProcessorStartMessage>(OnStartMessage);
            SubscribeLocalEvent<ProcessorComponent, ProcessorEjectChamberAllMessage>(OnEjectChamberAllMessage);
            SubscribeLocalEvent<ProcessorComponent, ProcessorEjectChamberContentMessage>(OnEjectChamberContentMessage);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveProcessorComponent, ProcessorComponent>();
            while (query.MoveNext(out var uid, out var active, out var processor))
            {
                if (active.EndTime > _timing.CurTime)
                    continue;

                processor.AudioStream = _audioSystem.Stop(processor.AudioStream);
                RemCompDeferred<ActiveProcessorComponent>(uid);

                var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedProcessor.InputContainerId);

                foreach (var item in inputContainer.ContainedEntities.ToList())
                {
                    var solution = active.Program switch
                    {
                        ProcessorProgram.Process => GetProcessResult(item),
                        _ => null,
                    };

                    if (solution is null)
                        continue;

                    if (TryComp<StackComponent>(item, out var stack))
                    {
                        /// IM Guessing you need to put the output of the recipe in here some where. You may want to remove the else, and do the process all in one statement.
                    }
                    else
                    {

                        /// This part is here is what is eating all the contents of the machine, and emptying the processor
                        var dev = new DestructionEventArgs();
                        RaiseLocalEvent(item, dev);

                        QueueDel(item);
                    }
                }

                _userInterfaceSystem.ServerSendUiMessage(uid, ProcessorUiKey.Key,
                    new ProcessorWorkCompleteMessage());

                UpdateUiState(uid);
            }
        }

        private void OnActiveProcessorStart(Entity<ActiveProcessorComponent> ent, ref ComponentStartup args)
        {
            _jitter.AddJitter(ent, -20, 20);
        }

        private void OnActiveProcessorRemove(Entity<ActiveProcessorComponent> ent, ref ComponentRemove args)
        {
            RemComp<JitteringComponent>(ent);
        }

        private void OnEntRemoveAttempt(Entity<ProcessorComponent> entity, ref ContainerIsRemovingAttemptEvent args)
        {
            if (HasComp<ActiveProcessorComponent>(entity))
                args.Cancel();
        }

        private void OnContainerModified(EntityUid uid, ProcessorComponent processor, ContainerModifiedMessage args)
        {
            UpdateUiState(uid);
        }

        private void OnInteractUsing(Entity<ProcessorComponent> entity, ref InteractUsingEvent args)
        {
            var heldEnt = args.Used;
            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedProcessor.InputContainerId);

            if (!HasComp<ExtractableComponent>(heldEnt))
            {
                if (!HasComp<FitsInDispenserComponent>(heldEnt))
                {
                    // This is ugly but we can't use whitelistFailPopup because there are 2 containers with different whitelists.
                    _popupSystem.PopupEntity(Loc.GetString("rocessor-component-cannot-put-entity-message"), entity.Owner, args.User);
                }

                // Entity did NOT pass the whitelist for processor.
                // Wouldn't want the CL grinding up the CO's ID card now would you?
                // Why am I asking you? You're biased.
                return;
            }

            if (args.Handled)
                return;

            // Cap the chamber. Don't want someone putting in 500 entities and ejecting them all at once.
            // Maybe I should have done that for the microwave too?
            if (inputContainer.ContainedEntities.Count >= entity.Comp.StorageMaxEntities)
                return;

            if (!_containerSystem.Insert(heldEnt, inputContainer))
                return;

            args.Handled = true;
        }

        private void UpdateUiState(EntityUid uid)
        {
            ProcessorComponent? processorComp = null;
            if (!Resolve(uid, ref processorComp))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedProcessor.InputContainerId);
            Solution? containerSolution = null;
            var isBusy = HasComp<ActiveProcessorComponent>(uid);
            var canProcess = false;

            if (inputContainer.ContainedEntities.Count > 0)
            {
                canProcess = inputContainer.ContainedEntities.All(CanProcess);
            }

            var state = new ProcessorInterfaceState(
                isBusy,
                this.IsPowered(uid, EntityManager),
                canProcess,
                GetNetEntityArray(inputContainer.ContainedEntities.ToArray()),
                containerSolution?.Contents.ToArray()
            );
            _userInterfaceSystem.SetUiState(uid, ProcessorUiKey.Key, state);
        }

        private void OnStartMessage(Entity<ProcessorComponent> entity, ref ProcessorStartMessage message)
        {
            if (!this.IsPowered(entity.Owner, EntityManager) || HasComp<ActiveProcessorComponent>(entity))
                return;

            DoWork(entity.Owner, entity.Comp, message.Program);
        }

        private void OnEjectChamberAllMessage(Entity<ProcessorComponent> entity, ref ProcessorEjectChamberAllMessage message)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedProcessor.InputContainerId);

            if (HasComp<ActiveProcessorComponent>(entity) || inputContainer.ContainedEntities.Count <= 0)
                return;

            ClickSound(entity);
            foreach (var toEject in inputContainer.ContainedEntities.ToList())
            {
                _containerSystem.Remove(toEject, inputContainer);
                _randomHelper.RandomOffset(toEject, 0.4f);
            }
            UpdateUiState(entity);
        }

        private void OnEjectChamberContentMessage(Entity<ProcessorComponent> entity, ref ProcessorEjectChamberContentMessage message)
        {
            if (HasComp<ActiveProcessorComponent>(entity))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(entity.Owner, SharedProcessor.InputContainerId);
            var ent = GetEntity(message.EntityId);

            if (_containerSystem.Remove(ent, inputContainer))
            {
                _randomHelper.RandomOffset(ent, 0.4f);
                ClickSound(entity);
                UpdateUiState(entity);
            }
        }

        /// <summary>
        /// The wzhzhzh of the processor. Processes the contents of the processor and ejects the output.
        /// </summary>
        /// <param name="uid">The processor itself</param>
        /// <param name="Processor"></param>
        /// <param name="program">the processor program</param>
        private void DoWork(EntityUid uid, ProcessorComponent Processor, ProcessorProgram program)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedProcessor.InputContainerId);

            // Do we have anything to process?
            if (inputContainer.ContainedEntities.Count <= 0 )
                return;

            SoundSpecifier? sound;
            switch (program)
            {
                case ProcessorProgram.Process when inputContainer.ContainedEntities.All(CanProcess):
                    sound = Processor.ProcessSound;
                    break;
                default:
                    return;
            }

            var active = AddComp<ActiveProcessorComponent>(uid);
            active.EndTime = _timing.CurTime + Processor.WorkTime * Processor.WorkTimeMultiplier;
            active.Program = program;

            Processor.AudioStream = _audioSystem.PlayPvs(sound, uid,
                AudioParams.Default.WithPitchScale(1 / Processor.WorkTimeMultiplier))?.Entity; //slightly higher pitched
            _userInterfaceSystem.ServerSendUiMessage(uid, ProcessorUiKey.Key, new ProcessorWorkStartedMessage(program));
        }

        private void ClickSound(Entity<ProcessorComponent> Processor)
        {
            _audioSystem.PlayPvs(Processor.Comp.ClickSound, Processor.Owner, AudioParams.Default.WithVolume(-2f));
        }

        private Solution? GetProcessResult(EntityUid uid)
        {
            if (TryComp<ExtractableComponent>(uid, out var extractable)
                && extractable.GrindableSolution is not null
                && _solutionContainersSystem.TryGetSolution(uid, extractable.GrindableSolution, out _, out var solution))
            {
                return solution;
            }
            else
                return null;
        }

        private bool CanProcess(EntityUid uid)
        {
            var solutionName = CompOrNull<ExtractableComponent>(uid)?.GrindableSolution;

            return solutionName is not null && _solutionContainersSystem.TryGetSolution(uid, solutionName, out _, out _);
        }

    }
}
