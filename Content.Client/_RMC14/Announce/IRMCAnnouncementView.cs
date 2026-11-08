namespace Content.Client._RMC14.Announce;

public interface IRMCAnnouncementView
{
    void Setup(RMCAnnouncementDisplay display);

    void SetTitleProgress(float progress);

    void SetBody(string shown, int fade, bool cursor, bool cursorVisible);

    void SetPower(float power);

    void SetPulse(float pulse);
}
