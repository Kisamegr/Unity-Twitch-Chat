using UnityEngine;

public class SimpleExample : MonoBehaviour
{
    private TwitchIRC IRC;
    public TwitchIRC.Chatter chatter;

    private void Awake()
    {
        //Place TwitchIRC.cs script on an gameObject called "TwitchIRC"
        IRC = GameObject.Find("TwitchIRC").GetComponent<TwitchIRC>();

        //Add an event listener
        IRC.newChatMessageEvent.AddListener(NewMessage);
    }

    //This gets called whenever a new chat message appears
    public void NewMessage(TwitchIRC.Chatter newChatter)
    {
        Debug.Log("New chatter object received!");

        chatter = newChatter;

        //Examples for using the chatter object:

        if (chatter.displayName == "Kisamegr")
            Debug.Log("Chat message was sent by Kisamegr!");

        if (chatter.HasBadge("subscriber"))
            Debug.Log("Chat message sender is a subscriber");

        if (chatter.HasBadge("moderator"))
            Debug.Log("Chat message sender is a channel moderator");

        if (chatter.MessageContainsEmote("25")) //25 = Kappa emote ID
            Debug.Log("Chat message contained the Kappa emote");

        //Etc...
    }
}
