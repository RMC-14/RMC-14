cmd-rmcdeletecommendations-desc = Deletes commendations by round, giver, receiver, or id.
cmd-rmcdeletecommendations-help = Usage:
  rmcdeletecommendations id <commendationId>
    - Deletes a single commendation by id

  rmcdeletecommendations round <roundId> <type>
    - Deletes all commendations for a specific round and type
    - type: type commendation filter

  rmcdeletecommendations round <roundId> <type> giver <usernameOrId>
    - Deletes commendations in a round and type given by a player
    - type: type commendation filter

  rmcdeletecommendations round <roundId> <type> receiver <usernameOrId>
    - Deletes commendations in a round and type received by a player
    - type: type commendation filter

  Examples:
    rmcdeletecommendations id 128
    rmcdeletecommendations round 42 medal
    rmcdeletecommendations round 42 jelly giver PlayerName
    rmcdeletecommendations round 42 medal receiver PlayerName

cmd-rmcdeletecommendations-invalid-arguments = Incorrect arguments!
cmd-rmcdeletecommendations-invalid-round-id = Invalid round ID!
cmd-rmcdeletecommendations-invalid-id = Invalid commendation ID!
cmd-rmcdeletecommendations-invalid-type = Invalid type '{ $type }'!
cmd-rmcdeletecommendations-invalid-player-mode = Invalid player mode! Must be 'giver' or 'receiver'.
cmd-rmcdeletecommendations-player-not-found = Player '{ $player }' not found.
cmd-rmcdeletecommendations-no-results = No commendations found.

cmd-rmcdeletecommendations-id-header = Deleted commendation { $id }:
cmd-rmcdeletecommendations-round-header = Deleted commendations for Round { $round } ({ $count } total):
cmd-rmcdeletecommendations-format = id [{ $id }] { $type }: { $name } - { $giverUserName } ({ $giver }) → { $receiverUserName } ({ $receiver }) Round { $round }: { $text }
cmd-rmcdeletecommendations-admin-announcement = { $admin } deleted commendations: { $ids }
cmd-rmcdeletecommendations-admin-announcement-round = { $admin } deleted commendations for Round { $round }: { $ids }

cmd-rmcdeletecommendations-hint-mode = Mode (id or round)
cmd-rmcdeletecommendations-hint-mode-id = Delete a commendation by id
cmd-rmcdeletecommendations-hint-mode-round = Delete commendations by round
cmd-rmcdeletecommendations-hint-round-id = Round ID
cmd-rmcdeletecommendations-hint-commendation-id = Commendation ID
cmd-rmcdeletecommendations-hint-type = Commendation type
cmd-rmcdeletecommendations-hint-player-mode = Player mode (giver or receiver)
cmd-rmcdeletecommendations-hint-player-giver = Commendations given by player
cmd-rmcdeletecommendations-hint-player-receiver = Commendations received by player
cmd-rmcdeletecommendations-hint-player = Player username or UserId
