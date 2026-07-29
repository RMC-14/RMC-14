command-description-mutiny-end = Ends the active mutiny and removes all mutiny state.
command-description-mutiny-ismutineer = Returns true if the entity is an active mutineer, false otherwise.
command-description-mutiny-list = Lists the active mutiny phase, leaders, recruits, and participants.
command-description-mutiny-makemutineer = Adds an accepted recruit or makes an entity a mutineer during an active mutiny.
command-description-mutiny-removemutineer = Removes an accepted recruit or makes an active mutineer a non-combatant.
command-description-mutiny-makemutineerleader = Creates or joins the global mutiny as a leader.
command-description-mutiny-removemutineerleader = Removes mutiny leadership without changing an active side.
command-description-mutiny-makeloyalist = Makes an entity a loyalist during an active mutiny.
command-description-mutiny-makenoncombatant = Makes an entity a non-combatant during an active mutiny.

ent-ActionMutineerRecruit = Recruit mutineer
    .desc = Ask an eligible marine to join the mutiny when it begins.
ent-ActionMutineerBegin = Begin mutiny
    .desc = Begin the mutiny with every accepted recruit.

mutineer-status-added = [bold][color=red]You are now a Mutineer![/color][/bold]
    Check the mutiny rules before participating.
mutineer-status-removed = You are no longer a mutineer.
mutineer-leader-status-added = [bold][color=red]You have been made a leader of the mutiny.[/color][/bold]
    Recruit supporters, then use Begin Mutiny when you are ready.
mutineer-leader-status-removed = You are no longer a leader of the mutiny.
rmc-mutiny-loyalist-status-added = [bold][color=red]You are now a Loyalist![/color][/bold]
    Check the mutiny rules before participating.
rmc-mutiny-noncombatant-status-added = [bold][color=red]You are now a Non-Combatant![/color][/bold]
    Do not take part in combat. You may treat either side, but must not engage or be engaged.

mutineer-invite-title = Mutiny invitation
mutineer-invite-text = You are being asked to join a mutiny.
    Read and understand the Mutinies and Riots guidelines (Core Rules -> "Mutinies, Riots") before accepting.
mutineer-invite-accept = Join
mutineer-invite-deny = Decline
rmc-mutiny-recruit-sent = Mutiny invitation sent.
rmc-mutiny-recruit-accepted = You will become a mutineer when the mutiny begins. Prepare, but do not cause harm before then.

rmc-mutiny-begin-title = Begin mutiny?
rmc-mutiny-begin-text = Are you sure you want to begin the mutiny?
rmc-mutiny-begin-accept = Begin
rmc-mutiny-begin-deny = Cancel

rmc-mutiny-side-title = Choose a side
rmc-mutiny-side-text = A mutiny has begun. With whom do you stand?
    Read and understand the Mutinies and Riots guidelines (Core Rules -> "Mutinies, Riots") before choosing a side.
    Closing this window or waiting 20 seconds means refusing to fight.
rmc-mutiny-side-mutineer = Mutineers
rmc-mutiny-side-loyalist = Loyalists
rmc-mutiny-side-refuse = Refuse to fight

rmc-mutiny-announcement = DANGER: Communications received; a mutiny is in progress. Code: Detain, Arrest, Defend.

rmc-mutiny-error-invalid-member = The target is not a valid UNMC mutiny member.
rmc-mutiny-error-invalid-recruit = That marine cannot be recruited into the mutiny.
rmc-mutiny-error-rule = The mutiny game rule could not be started.
rmc-mutiny-error-other-rule = That mind already belongs to another mutiny.
rmc-mutiny-error-no-rule = There is no active mutiny. Assign a mutiny leader first.
rmc-mutiny-error-not-active = The mutiny has not begun.
rmc-mutiny-error-not-recruiting = The mutiny is no longer recruiting.
rmc-mutiny-error-not-leader = The target is not a mutiny leader.
rmc-mutiny-error-not-mutineer = The target is not a mutineer or accepted recruit.
rmc-mutiny-error-leader-side = Remove mutiny leadership before assigning this side.
rmc-mutiny-error-remove-leader-first = Remove mutiny leadership before removing mutineer status.

rmc-mutiny-admin-leader-added = {$player} was made a mutiny leader.
rmc-mutiny-admin-leader-removed = {$player} is no longer a mutiny leader.
rmc-mutiny-admin-recruit-accepted = {$target} accepted a mutiny invitation from {$leader}.
rmc-mutiny-admin-begun = {$leader} began the mutiny.

rmc-mutiny-verb-make-leader = Make mutiny leader
rmc-mutiny-verb-remove-leader = Remove mutiny leader
rmc-mutiny-verb-make-mutineer = Make mutineer
rmc-mutiny-verb-remove-mutineer = Remove mutineer

rmc-mutiny-command-success = Mutiny state updated.
rmc-mutiny-command-list-none = There is no active mutiny.
rmc-mutiny-command-list-header = Active mutiny phase: {$phase}
rmc-mutiny-phase-recruiting = Recruiting
rmc-mutiny-phase-active = Active
rmc-mutiny-side-name-mutineer = Mutineer
rmc-mutiny-side-name-loyalist = Loyalist
rmc-mutiny-side-name-noncombatant = Non-combatant
rmc-mutiny-command-list-recruit = Accepted recruit
rmc-mutiny-command-list-unassigned = Unassigned
rmc-mutiny-command-list-leader = , leader
rmc-mutiny-command-list-entry = - {$player}: {$state}{$leader}
