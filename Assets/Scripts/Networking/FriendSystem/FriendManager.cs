using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
public class FriendManager
{
   List<string> friends = new List<string>();   
    public int FriendCount => friends.Count;
    public void AddFriend(string userID)
   {
       if (!friends.Contains(friendId))
       {
           friends.Add(friendId);
           Debug.Log($"Friend {friendId} added.");
       }
       else
       {
           Debug.Log($"Friend {friendId} is already in the list.");
       }
    }

    public void RemoveFriend(string userID)
    {
        if (friends.Contains(friendId))
        {
            friends.Remove(friendId);
            Debug.Log($"Friend {friendId} removed.");
        }
        else
        {
            Debug.Log($"Friend {friendId} not found in the list.");
        }
    }  

    public bool IsFriend(string userID)
    {
        return friends.Contains(userID);
    }

    public void Search(string userID)
    {

    }
    public string GetFriend()
    {
        return null;
    }
    public string[] GetFriends()
    {
        return null;
    }
}

public class MakeFriend 
{
    List<string> friendRequests = new List<string>();
    List<string> pendingRequests = new List<string>();
    public void SendFriendRequest(string userID)
    {

    }
    public void AcceptFriendRequest(string userID)
    {

    }
    public void DeclineFriendRequest(string userID)
    {

    }

    public void CancelFriendRequest(string userID)
    {

    }

}

