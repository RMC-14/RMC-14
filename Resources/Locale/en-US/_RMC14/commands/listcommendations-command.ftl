# List Commendations Command
cmd-listcommendations-desc = Lists commendations by round, player, or recent entries.
cmd-listcommendations-help = Usage:
  listcommendations last <count> [type]
    - Lists the most recent commendations
    - count: number of most recent commendations to show
    - type: all (default), medal, or jelly
  
  listcommendations round <roundId> [type]
    - Lists all commendations for a specific round
    - type: all (default), medal, or jelly
  
  listcommendations player giver <usernameOrId> <count> [type]
    - Lists commendations given by a player
    - count: number of most recent commendations to show
    - type: all (default), medal, or jelly
  
  listcommendations player receiver <usernameOrId> <count> [type]
    - Lists commendations received by a player
    - count: number of most recent commendations to show
    - type: all (default), medal, or jelly
  
  Examples:
    listcommendations last 10
    listcommendations last 5 all
    listcommendations round 42
    listcommendations round 42 medal
    listcommendations player giver PlayerName 10
    listcommendations player receiver PlayerName 5 jelly

# Errors
cmd-listcommendations-invalid-arguments = Incorrect arguments!
cmd-listcommendations-invalid-round-id = Invalid round ID!
cmd-listcommendations-invalid-type = Invalid type '{ $type }'! Must be 'all', 'medal', or 'jelly'.
cmd-listcommendations-invalid-player-mode = Invalid player mode! Must be 'giver' or 'receiver'.
cmd-listcommendations-invalid-count = Invalid count! Must be a positive number.
cmd-listcommendations-player-not-found = Player '{ $player }' not found.
cmd-listcommendations-no-results = No commendations found.

# Headers
cmd-listcommendations-last-header = Showing { $count } most recent commendations (requested: { $total }):
cmd-listcommendations-round-header = Commendations for Round { $round } ({ $count } total):
cmd-listcommendations-giver-header = Showing { $count } most recent commendations given (requested: { $total }):
cmd-listcommendations-receiver-header = Showing { $count } most recent commendations received (requested: { $total }):

# Format
cmd-listcommendations-format = [{ $id }] { $type }: { $name } - { $giverUserName } ({ $giver }) → { $receiverUserName } ({ $receiver }) Round { $round }: { $text }

# Completion hints
cmd-listcommendations-hint-mode = Mode (last, round, or player)
cmd-listcommendations-hint-mode-last = List most recent commendations
cmd-listcommendations-hint-mode-round = List commendations by round
cmd-listcommendations-hint-mode-player = List commendations by player
cmd-listcommendations-hint-round-id = Round ID
cmd-listcommendations-hint-player-mode = Player mode (giver or receiver)
cmd-listcommendations-hint-player-giver = Commendations given by player
cmd-listcommendations-hint-player-receiver = Commendations received by player
cmd-listcommendations-hint-player = Player username or UserId
cmd-listcommendations-hint-count = Number of commendations to show
cmd-listcommendations-hint-type = Type filter (all, medal, or jelly)
cmd-listcommendations-hint-type-all = Show all types
cmd-listcommendations-hint-type-medal = Show only medals
cmd-listcommendations-hint-type-jelly = Show only jellies
