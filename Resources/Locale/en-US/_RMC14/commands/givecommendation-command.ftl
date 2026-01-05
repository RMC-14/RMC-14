# Give Commendation Command
cmd-givecommendation-desc = Awards a medal or jelly to a player
cmd-givecommendation-help = Usage: givecommendation <giverName> <receiver> <receiverName> <type> <commendationType> <citation> [roundId]
  Arguments:
  giverName: who is giving IC the award (MUST use quotes if contains spaces)
  receiver: player username or UserId
  receiverName: character name (MUST use quotes if contains spaces)
  type: medal or jelly
  commendationType: a number (use tab completion to see available types)
  citation: the reason for the award (MUST be in quotes)
  roundId: round number, defaults to current round (optional)
  
  Examples:
    givecommendation "UNMC High Command" PlayerName "John Doe" medal 1 "For exceptional bravery"
    givecommendation "The Queen Mother" XenoPlayer "XX-Alpha" jelly 2 "For defending the hive"
    givecommendation "UNMC High Command" PlayerName "John Doe" medal 1 "For exceptional bravery" 42

# Errors
cmd-givecommendation-invalid-arguments = Incorrect number of arguments!
cmd-givecommendation-invalid-type = Invalid type! Must be 'medal' or 'jelly'.
cmd-givecommendation-invalid-award-type = Invalid '{ $type }' type! Must be 1-{ $max }.
cmd-givecommendation-empty-citation = Citation cannot be empty!
cmd-givecommendation-player-not-found = Player '{ $player }' not found.

# Success
cmd-givecommendation-success = { $award } awarded to { $player }!
cmd-givecommendation-admin-announcement = { $admin } awarded { $type } "{ $award }" to { $receiver } (character: { $character }) for Round { $round }

# Completion hints
cmd-givecommendation-hint-giver = Giver IC name (be careful when entering the IC name)
cmd-givecommendation-hint-giver-highcommand = Standard giver for marine medals
cmd-givecommendation-hint-giver-queen-mother = Standard giver for xeno jellies
cmd-givecommendation-hint-receiver = Receiver username or UserId
cmd-givecommendation-hint-receiver-name = Receiver character name (be careful when entering the IC name)
cmd-givecommendation-hint-type = Type (medal or jelly)
cmd-givecommendation-hint-type-medal = Award a medal to a marine
cmd-givecommendation-hint-type-jelly = Award a royal jelly to a xeno
cmd-givecommendation-hint-medal-type = Medal type (1-{ $count })
cmd-givecommendation-hint-jelly-type = Jelly type (1-{ $count })
cmd-givecommendation-hint-invalid-type = Type must be 'medal' or 'jelly'
cmd-givecommendation-hint-citation = Citation text (be careful when entering the IC reason)
cmd-givecommendation-hint-round = Round ID (optional)
cmd-givecommendation-hint-round-current = Current round