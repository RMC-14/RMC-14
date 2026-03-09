# List Commendations Command
cmd-rmclistcommendations-desc = Lists commendations by round, player, id, or recent entries.
cmd-rmclistcommendations-help = Usage:
  rmclistcommendations last <count> [type]
    - Lists the most recent commendations
    - count: number of most recent commendations to show
    - type: type commendation filter (all default)
  
  rmclistcommendations round <roundId> [type]
    - Lists all commendations for a specific round
    - type: type commendation filter (all default)

  rmclistcommendations id <commendationId>
    - Lists a single commendation by id
  
  rmclistcommendations player giver <usernameOrId> <count> [type]
    - Lists commendations given by a player
    - count: number of most recent commendations to show
    - type: type commendation filter (all default)
  
  rmclistcommendations player receiver <usernameOrId> <count> [type]
    - Lists commendations received by a player
    - count: number of most recent commendations to show
    - type: type commendation filter (all default)
  
  Examples:
    rmclistcommendations last 10
    rmclistcommendations last 5 jelly
    rmclistcommendations round 42
    rmclistcommendations round 42 medal
    rmclistcommendations id 128
    rmclistcommendations player giver PlayerName 10
    rmclistcommendations player receiver PlayerName 5 jelly

# Errors
cmd-rmclistcommendations-invalid-arguments = Incorrect arguments!
cmd-rmclistcommendations-invalid-round-id = Invalid round ID!
cmd-rmclistcommendations-invalid-id = Invalid commendation ID!
cmd-rmclistcommendations-invalid-type = Invalid type '{ $type }'!
cmd-rmclistcommendations-invalid-player-mode = Invalid player mode! Must be 'giver' or 'receiver'.
cmd-rmclistcommendations-invalid-count = Invalid count! Must be a positive number.
cmd-rmclistcommendations-player-not-found = Player '{ $player }' not found.
cmd-rmclistcommendations-no-results = No commendations found.

# Headers
cmd-rmclistcommendations-last-header = Showing { $count } most recent commendations (requested: { $total }):
cmd-rmclistcommendations-round-header = Commendations for Round { $round } ({ $count } total):
cmd-rmclistcommendations-id-header = Commendation { $id }:
cmd-rmclistcommendations-giver-header = Showing { $count } most recent commendations given (requested: { $total }):
cmd-rmclistcommendations-receiver-header = Showing { $count } most recent commendations received (requested: { $total }):

# Format
cmd-rmclistcommendations-format = id [{ $id }] { $type }: { $name } - { $giverUserName } ({ $giver }) → { $receiverUserName } ({ $receiver }) Round { $round }: { $text }

# Completion hints
cmd-rmclistcommendations-hint-mode = Mode (last, round, id, or player)
cmd-rmclistcommendations-hint-mode-last = List most recent commendations
cmd-rmclistcommendations-hint-mode-round = List commendations by round
cmd-rmclistcommendations-hint-mode-id = List a commendation by id
cmd-rmclistcommendations-hint-mode-player = List commendations by player
cmd-rmclistcommendations-hint-round-id = Round ID
cmd-rmclistcommendations-hint-commendation-id = Commendation ID
cmd-rmclistcommendations-hint-player-mode = Player mode (giver or receiver)
cmd-rmclistcommendations-hint-player-giver = Commendations given by player
cmd-rmclistcommendations-hint-player-receiver = Commendations received by player
cmd-rmclistcommendations-hint-player = Player username or UserId
cmd-rmclistcommendations-hint-count = Number of commendations to show
cmd-rmclistcommendations-hint-type = Type commendation filter
