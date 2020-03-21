using System.Collections.Generic;
using System.Collections;
using System.Net.Sockets;
using System.IO;
using UnityEngine;

public class TwitchIRC : MonoBehaviour
{
    [HideInInspector] public class NewChatMessageEvent : UnityEngine.Events.UnityEvent<Chatter> { }

    public NewChatMessageEvent newChatMessageEvent = new NewChatMessageEvent();

    private TcpClient client;
    private NetworkStream stream;
    private StreamReader reader;
    private StreamWriter writer;

    public Settings settings;
    public UserInput details;

    private bool connected = false;

    [System.Serializable]
    public class UserInput
    {
        public string inputOauth = string.Empty;
        public string inputNick = string.Empty;
        public string inputChannel = string.Empty;
    }

    [System.Serializable]
    public class Settings
    {
        public string server = "irc.chat.twitch.tv";
        public int port = 6667;

        public bool connectOnStart = true;
        public bool parseBadges = true;
        public bool parseTwitchEmotes = true;
        public bool allowSymbolsInNames = false;
        public bool debugIRC = false;
    }

  private static TwitchIRC _instance = null;
  public static TwitchIRC Shared { get => _instance; }
    private void Awake()
    {
        _instance = this;
        if (settings.connectOnStart)
            ConnectIRC();
    }

    private void Update()
    {
        ReadInput();
        WriteOutput();
    }

    private void OnDisable()
    {
        DisconnectIRC();
    }

    [ContextMenu("Connect IRC")]
    public void ConnectIRC()
    {
        if (details.inputOauth == string.Empty || details.inputNick == string.Empty || details.inputChannel == string.Empty)
        {
            Debug.LogError("Error! Missing required information");
            return;
        }

        client = new TcpClient(settings.server, settings.port);
        stream = client.GetStream();

        reader = new StreamReader(stream);
        writer = new StreamWriter(stream);

        writer.WriteLine("PASS oauth:" + (details.inputOauth.StartsWith("oauth:") ? details.inputOauth.Substring(6).ToLower() : details.inputOauth.ToLower()));
        writer.WriteLine("NICK " + details.inputNick.ToLower());
        writer.WriteLine("CAP REQ :twitch.tv/tags"); //This is required for things like name color, badges, emotes etc.
        writer.Flush();

        connected = true;
        Debug.Log("Connected to Twitch IRC");
    }

    [ContextMenu("Disconnect IRC")]
    public void DisconnectIRC()
    {
		if (!connected) return;
		
        connected = false;

        client.Close();
        stream.Close();
        reader.Close();
        writer.Close();

        Debug.LogWarning("Disconnected from Twitch IRC");
    }

    public void SendCommand(string command)
    {
        outputQueue.Enqueue(command); //Place command in queue
    }

    /// <summary>
    /// Sends a chat message
    /// </summary>
    public void SendChatMessage(string message)
    {
        if (message.Length <= 0) //Message can't be empty
            return;

        outputQueue.Enqueue("PRIVMSG #" + details.inputChannel + " :" + message); //Place message in queue
    }

    private Queue<string> outputQueue = new Queue<string>();
    private bool outputCooldown;
    private void WriteOutput()
    {
        if (!connected || outputQueue.Count <= 0 || outputCooldown)
            return;

        string output = outputQueue.Dequeue(); //Get the first output in queue and remove it from the queue

        if (settings.debugIRC)
            Debug.Log("Sending command: " + output);

        //Send the output
        writer.WriteLine(output);
        writer.Flush();

        //Start a cooldown (1750 ms)
        outputCooldown = true;
        StartCoroutine(Cooldown());
    }

    private readonly WaitForSeconds delay = new WaitForSeconds(1.750f);
    private IEnumerator Cooldown()
    {
        yield return delay;

        outputCooldown = false;
    }

    private string buffer;
    private void ReadInput()
    {
        if (!connected || !stream.DataAvailable)
            return;

        buffer = reader.ReadLine();

        if (settings.debugIRC)
            Debug.Log(buffer);

        //Parse chat messages
        if (buffer.Contains("PRIVMSG #"))
        {
            //Send a new Chatter object to event listeners
            newChatMessageEvent.Invoke(ParseIRCMessage(buffer));
            return;
        }

        //About once every five minutes, the server will send you a PING :tmi.twitch.tv. To ensure that your connection to the server is not prematurely terminated, reply with PONG :tmi.twitch.tv.
        if (buffer.Contains("PING :tmi.twitch.tv"))
        {
            SendCommand("PONG :tmi.twitch.tv");
            return;
        }

        //Join channel
        if (buffer.Contains(":tmi.twitch.tv 001"))
        {
            SendCommand("JOIN #" + details.inputChannel.ToLower());
            Debug.Log("Joined Twitch channel: " + details.inputChannel.ToLower());
            return;
        }
    }

    private Chatter ParseIRCMessage(string message)
    {
        //Define Chatter object variables
        string colorHex = string.Empty, displayName = string.Empty, msg = string.Empty, channel = string.Empty;
        Chatter.Badge[] badges = null;
        List<Chatter.Emote> emotes = new List<Chatter.Emote>();

        //Parse Twitch badges
        if (settings.parseBadges)
        {
            string stringBadges = message.Substring(message.IndexOf("badges=") + 7);
            stringBadges = stringBadges.Substring(0, stringBadges.IndexOf(';'));

            string[] badgeStrings = stringBadges.Length > 0 ? stringBadges.Split(',') : new string[0];

            badges = new Chatter.Badge[badgeStrings.Length];

            for (int i = 0; i < badgeStrings.Length; ++i)
            {
                string s = badgeStrings[i];

                badges[i].id = s.Substring(0, s.IndexOf('/'));
                badges[i].version = s.Substring(s.IndexOf('/') + 1);
            }
        }

        //Parse Twitch emotes
        if (settings.parseTwitchEmotes)
        {
            string stringEmotes = message.Substring(message.IndexOf("emotes=") + 7);
            stringEmotes = stringEmotes.Substring(0, stringEmotes.IndexOf(';'));

            string[] emoteStrings = stringEmotes.Length > 0 ? stringEmotes.Split('/') : new string[0];

            for (int i = 0; i < emoteStrings.Length; ++i)
            {
                string s = emoteStrings[i];

                string[] indexes = s.Substring(s.IndexOf(':') + 1).Length > 0 ? s.Substring(s.IndexOf(':') + 1).Split(',') : new string[0];

                Chatter.Emote.Index[] ind = new Chatter.Emote.Index[indexes.Length];

                for (int j = 0; j < ind.Length; ++j)
                {
                    ind[j].startIndex = int.Parse(indexes[j].Substring(0, indexes[j].IndexOf('-')));
                    ind[j].endIndex = int.Parse(indexes[j].Substring(indexes[j].IndexOf('-') + 1));
                }

                //Add emote
                emotes.Add(new Chatter.Emote()
                {
                    id = s.Substring(0, s.IndexOf(':')),
                    indexes = ind
                });
            }
        }

        //Parse name color
        colorHex = message.Substring(message.IndexOf("color=") + 6);
        colorHex = colorHex.Substring(0, colorHex.IndexOf(';'));

        //Parse display-name
        displayName = message.Substring(message.IndexOf("display-name=") + 13);
        displayName = displayName.Substring(0, displayName.IndexOf(';'));

        //If unusual characters/symbols are present in user's display-name then use the actual login name instead (login name is always lowercase)
        if (!settings.allowSymbolsInNames && !System.Text.RegularExpressions.Regex.IsMatch(displayName, @"^[a-zA-Z0-9_]+$"))
        {
            displayName = message.Substring(message.IndexOf("user-type=") + 12);
            displayName = displayName.Substring(0, displayName.IndexOf('!'));
        }

        //Parse message content and channel name
        msg = message.Substring(message.IndexOf("PRIVMSG"));
        channel = msg.Substring(msg.IndexOf('#') + 1);
        channel = channel.Substring(0, channel.IndexOf(' '));
        msg = msg.Substring(msg.IndexOf(':') + 1);

        //Sort emotes to match emote order with the chat message
        if (settings.parseTwitchEmotes)
            emotes.Sort((a, b) => 1 * a.indexes[0].startIndex.CompareTo(b.indexes[0].startIndex));

        return new Chatter(colorHex, displayName, badges, emotes, msg, channel);
    }

    [System.Serializable]
    public class Chatter
    {
        public Color color; //Chatter's name color
        public string displayName; //Chatter's name
        public string message; //Chat message content
        public string channel; //The channel where this message was sent in

        [Header("Badge data")]
        public Badge[] badges; //All the badges and versions the chatter has displayed

        [Header("Emote data")]
        public List<Emote> emotes; //Twitch emotes included in the message (note: there is no support for ffz or bttv emotes)

        [System.Serializable]
        public struct Emote
        {
            [System.Serializable]
            public struct Index
            {
                public int startIndex, endIndex;
            }

            public string id;
            public Index[] indexes;
        }

        public bool MessageContainsEmote(string emoteId)
        {
            foreach (Emote e in emotes)
            {
                if (e.id == emoteId)
                    return true;
            }

            return false;
        }

        [System.Serializable]
        public struct Badge
        {
            public string id;
            public string version;
        }

        public bool HasBadge(string badgeName)
        {
            foreach (Badge b in badges)
            {
                if (b.id == badgeName)
                    return true;
            }

            return false;
        }

        public Chatter(string HexColor, string DisplayName, Badge[] Badges, List<Emote> Emotes, string Message, string Channel)
        {
            //Parse HEX value to RGBA color
            ColorUtility.TryParseHtmlString(HexColor, out color);

            displayName = DisplayName;
            badges = Badges;
            emotes = Emotes;
            message = Message;
            channel = Channel;
        }
    }
}