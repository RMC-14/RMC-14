namespace Content.Client._RMC14.Announce;

public interface IRMCAnnouncementView
{
    void Setup(RMCAnnouncementDisplay display);

    void SetBody(string text);
}
