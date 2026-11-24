# Give Commendation Command
cmd-rmcgivecommendation-desc = Awards a medal or jelly to a player
cmd-rmcgivecommendation-help = Usage: rmcgivecommendation <giverName> <receiver> <receiverName> <type> <commendationType> <citation> [roundId]
  Arguments:
  giverName: who is giving IC the award (MUST use quotes if contains spaces)
  receiver: player username or UserId
  receiverName: character name (MUST use quotes if contains spaces)
  type: medal or jelly
  commendationType: a number (use tab completion to see available types)
  citation: the reason for the award (MUST be in quotes)
  roundId: round number, defaults to current round (optional)
  
  Examples:
    rmcgivecommendation "UNMC High Command" PlayerName "John Doe" medal 1 "For exceptional bravery"
    rmcgivecommendation "The Queen Mother" XenoPlayer "XX-Alpha" jelly 2 "For defending the hive"
    rmcgivecommendation "UNMC High Command" PlayerName "John Doe" medal 1 "For exceptional bravery" 42

# Errors
cmd-rmcgivecommendation-invalid-arguments = Incorrect number of arguments!
cmd-rmcgivecommendation-invalid-type = Invalid type! Must be 'medal' or 'jelly'.
cmd-rmcgivecommendation-invalid-award-type = Invalid '{ $type }' type! Must be 1-{ $max }.
cmd-rmcgivecommendation-empty-citation = Citation cannot be empty!
cmd-rmcgivecommendation-player-not-found = Player '{ $player }' not found.

# Success
cmd-rmcgivecommendation-success = { $award } awarded to { $player }!
cmd-rmcgivecommendation-admin-announcement = { $admin } awarded { $type } "{ $award }" to { $receiver } (character: { $character }) for Round { $round }

# Completion hints
cmd-rmcgivecommendation-hint-giver = Giver IC name (be careful when entering the IC name)
cmd-rmcgivecommendation-hint-giver-highcommand = Standard giver for marine medals
cmd-rmcgivecommendation-hint-giver-queen-mother = Standard giver for xeno jellies
cmd-rmcgivecommendation-hint-receiver = Receiver username or UserId
cmd-rmcgivecommendation-hint-receiver-name = Receiver character name (be careful when entering the IC name)
cmd-rmcgivecommendation-hint-type = Type (medal or jelly)
cmd-rmcgivecommendation-hint-type-medal = Award a medal to a marine
cmd-rmcgivecommendation-hint-type-jelly = Award a royal jelly to a xeno
cmd-rmcgivecommendation-hint-medal-type = Medal type (1-{ $count })
cmd-rmcgivecommendation-hint-jelly-type = Jelly type (1-{ $count })
cmd-rmcgivecommendation-hint-invalid-type = Type must be 'medal' or 'jelly'
cmd-rmcgivecommendation-hint-citation = Citation text (be careful when entering the IC reason)
cmd-rmcgivecommendation-hint-round = Round ID (optional)
cmd-rmcgivecommendation-hint-round-current = Current round