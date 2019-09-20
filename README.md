# Unity-Twitch-Chat-IRC

This is a lightweight Twitch.tv IRC client for Unity

TwitchIRC parses chat messages into easy-to-use Chatter objects

It also parses additional information such as: name color, badges and emotes

![img](https://i.imgur.com/rmZpBbR.png)

## Requirements
1. Twitch account
2. Twitch OAuth token which you can get from https://twitchapps.com/tmi/
3. Twitch channel name

## Usage

**I recommend looking at SimpleExample.cs for usage examples.**

1. Create a new empty gameObject and add TwitchIRC.cs on it
2. Enter your OAuth token, Twitch username and the name of a channel you want to use in the Unity inspector of TwitchIRC
3. Create a new empty gameObject and a new C# script with an reference to the TwitchIRC.cs component
4. Add a listener to TwitchIRC.newChatMessageEvent 



## Simple example project

*Spawns chatters as boxes that jump around. Box color is determined by chat badges*

![img](https://i.imgur.com/2TuI78H.gif)
