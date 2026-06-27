using System;
public enum OnlineStatus 
{
    Online,
    Offline,
    InParty,
    InMatch
}

[Serializable]
public class Presence 
{
    public string Name;
    public string Tag; 
    public string AvatarUrl;
    public OnlineStatus Status;
}
