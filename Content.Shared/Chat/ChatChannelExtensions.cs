﻿namespace Content.Shared.Chat;

public static class ChatChannelExtensions
{
    public static Color TextColor(this ChatChannel channel)
    {
        return channel switch
        {
            ChatChannel.Server => Color.Orange,
            ChatChannel.Radio => Color.LimeGreen,
            ChatChannel.LOOC => Color.MediumTurquoise,
            ChatChannel.OOC => Color.LightSkyBlue,
            ChatChannel.Dead => Color.MediumPurple,
            ChatChannel.Admin => Color.Red,
            ChatChannel.AdminAlert => Color.Red,
            ChatChannel.AdminChat => Color.HotPink,
            ChatChannel.MentorChat => Color.Orange,
            ChatChannel.Whisper => Color.DarkGray,
            _ => Color.LightGray
        };
    }
}
