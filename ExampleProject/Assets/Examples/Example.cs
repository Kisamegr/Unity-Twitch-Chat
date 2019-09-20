using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour
{
    // This is a simple example on how you could use the Chatter objects
    //
    // Examples at lines 49-64

    private TwitchIRC IRC;

    private void Awake()
    {
        IRC = GameObject.Find("TwitchIRC").GetComponent<TwitchIRC>();

        //Add an event listener
        IRC.newChatMessageEvent.AddListener(NewMessage);

        StartCoroutine(SpawnerLoop());
    }

    private Queue<TwitchIRC.Chatter> chatterQueue = new Queue<TwitchIRC.Chatter>();

    //This gets called whenever a new chat message appears
    public void NewMessage(TwitchIRC.Chatter chatter)
    {
        chatterQueue.Enqueue(chatter);
    }

    public GameObject boxPrefab;
    private IEnumerator SpawnerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (chatterQueue.Count <= 0)
                continue;

            TwitchIRC.Chatter chatter = chatterQueue.Dequeue();

            GameObject o = Instantiate(boxPrefab, transform.position, Quaternion.identity);

            string boxName = chatter.displayName;
            Color boxColor = Color.white;
            float boxScale = 1f;

            //Check if chatter is a subscriber, if true make their box color magenta
            if (chatter.HasBadge("subscriber"))
                boxColor = Color.magenta;

            //...or if the chatter is a moderator, make the box green!
            if (chatter.HasBadge("moderator"))
                boxColor = Color.green;

            //You can see the full list of badge names here: (JSON data)
            //https://badges.twitch.tv/v1/badges/global/display?language=en


            //If the chatter's chat message contains the emote Kappa then let's double the size of the box
            //Kappa emote's ID is = 25
            if (chatter.MessageContainsEmote("25"))
                boxScale = 2f;

            o.GetComponent<Box>().Initialize(boxName, boxColor, boxScale);
        }
    }
}
