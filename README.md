## Features

* Image Logging for Signage / Photo Frames / Fireworks / Carvable Pumpkins / Neon Signs / Sign Artist Updates to Discord
* Customizable Discord Buttons to take action against the entity / player that updated the sign
    * Buttons Can Locked Behind Discord Roles / Oxide Groups (Oxide Groups Requires a Discord Link Plugin)
    * Logging of which user pressed a button and the command that was run
    * Server / Player messages when a button is pressed
* Permanent / Temporary Image Update Bans
* Erasing Sign Image / Replacing Sign Image With Configurable Image or Text
* Fully Customizable Discord Embed Message
* Sign banning / unbanning

### Sign Update
![](https://i.postimg.cc/R0h6c3Z5/image.png)

### Pattern Firework Update
![](https://i.postimg.cc/WzNPkZs0/image.png)

### Sign Artist URL
![](https://i.postimg.cc/pVqwSp21/image.png)

### Erased Replaced Image
![](https://i.postimg.cc/jj4qtcYq/image.png)

### Action Log
![](https://i.postimg.cc/BvBY1Vbw/image.png)

## Discord Link
This plugin supports Discord Link provided by the Discord Extension.
This plugin will work with any plugin that provides linked player data through Discord Link.

## Linux Users
Please see this post to make sure you have required dependencies installed  
[https://umod.org/community/sign-artist/5685-sign-artist-faq](https://umod.org/community/sign-artist/5685-sign-artist-faq)

## Getting Your Bot Token
[Click Here to learn how to get an Discord Bot Token](https://umod.org/extensions/discord#getting-your-api-key)

## Configuration

```json
{
  "Discord Bot Token": "",
  "Disable Discord Button After Use": true,
  "Action Log Channel ID": "",
  "Replace Erased Image (Requires SignArtist)": {
    "Replaced Mode (None, Url, Text)": "Url",
    "URL": "https://i.postimg.cc/mD5xZ5R5/Erased-4.png",
    "Message": "ERASED BY ADMIN",
    "Font Size": 16,
    "Text Color": "#cd4632",
    "Body Color": "#000000"
  },
  "Firework Settings": {
    "Image Size (Pixels)": 250,
    "Circle Size (Pixels)": 19
  },
  "Sign Messages": [
    {
      "Message ID": "DEFAULT",
      "Discord Channel ID": "931295303640961135",
      "Use Action Button": true,
      "Buttons": [
        "ERASE",
        "SIGN_BLOCK_24_HOURS",
        "KILL_ENTITY",
        "KICK_PLAYER",
        "BAN_PLAYER"
      ]
    }
  ],
  "Buttons": [
    {
      "Button ID": "ERASE",
      "Button Display Name": "Erase",
      "Button Style": "Primary",
      "Commands": [
        "dsl.erase {discordsignlogger.entity.id} {discordsignlogger.entity.textureindex}"
      ],
      "Player Message": "An admin erased your sign for being inappropriate",
      "Server Message": "",
      "Show Confirmation Modal": false,
      "Requires Permissions To Use Button": false,
      "Allowed Discord Roles (Role ID)": [],
      "Allowed Oxide Groups (Group Name)": []
    },
    {
      "Button ID": "SIGN_BLOCK_24_HOURS",
      "Button Display Name": "Sign Block (24 Hours)",
      "Button Style": "Primary",
      "Commands": [
        "dsl.signblock {player.id} 86400"
      ],
      "Player Message": "You have been banned from updating signs for 24 hours.",
      "Server Message": "",
      "Show Confirmation Modal": false,
      "Requires Permissions To Use Button": true,
      "Allowed Discord Roles (Role ID)": [],
      "Allowed Oxide Groups (Group Name)": []
    },
    {
      "Button ID": "KILL_ENTITY",
      "Button Display Name": "Kill Entity",
      "Button Style": "Secondary",
      "Commands": [
        "entid kill {discordsignlogger.entity.id}"
      ],
      "Player Message": "An admin killed your sign for being inappropriate",
      "Server Message": "",
      "Show Confirmation Modal": true,
      "Requires Permissions To Use Button": false,
      "Allowed Discord Roles (Role ID)": [],
      "Allowed Oxide Groups (Group Name)": []
    },
    {
      "Button ID": "KICK_PLAYER",
      "Button Display Name": "Kick Player",
      "Button Style": "Danger",
      "Commands": [
        "kick {player.id} \"{discordsignlogger.message.player}\"",
        "dsl.erase {discordsignlogger.entity.id} {discordsignlogger.entity.textureindex}"
      ],
      "Player Message": "",
      "Server Message": "",
      "Show Confirmation Modal": true,
      "Requires Permissions To Use Button": false,
      "Allowed Discord Roles (Role ID)": [],
      "Allowed Oxide Groups (Group Name)": []
    },
    {
      "Button ID": "BAN_PLAYER",
      "Button Display Name": "Ban Player",
      "Button Style": "Danger",
      "Commands": [
        "ban {player.id} \"{discordsignlogger.message.player}\"",
        "dsl.erase {discordsignlogger.entity.id} {discordsignlogger.entity.textureindex}"
      ],
      "Player Message": "",
      "Server Message": "",
      "Show Confirmation Modal": true,
      "Requires Permissions To Use Button": false,
      "Allowed Discord Roles (Role ID)": [],
      "Allowed Oxide Groups (Group Name)": []
    }
  ],
  "PluginSettings": {
    "Sign Artist Settings": {
      "Log /sil": true,
      "Log /sili": true,
      "Log /silt": true
    }
  },
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

**Note**: You can use Thread Channel ID's for Channel ID's as of version 1.0.8

### Button Commands
Button commands are run as server console commands. Any command that can be ran from the console will work as a button command

### Available Discord Button Styles

Below is a list of button styles that can be used for your "Button Style" in the config

##### Primary
![](https://i.postimg.cc/VLFcS6H4/image.png)
#### Secondary
![](https://i.postimg.cc/Y9BXfNvF/image.png)
#### Success
![](https://i.postimg.cc/sX5Nt6kK/image.png)
#### Danger
![](https://i.postimg.cc/8zqwNdBY/image.png)
#### Link
![](https://i.postimg.cc/3xCVgRbB/image.png)

## Commands

### Server
`dsl.erase {entityId} {textureIndex}` - Will erase the image from the entity id with the given index  
`dsl.signblock {playerId} {durationInSeconds}`  - Will block a player from updating a sign for specified minutes. If no time is specified block will be permanent.  
`dsl.signunblock {playerId}` - Will unblock a previous blocked player. Allowing them to update signs again.

### Discord
`/dsl block - blocks the player from updating signs`
`/dsl unblock - allows previously blocked player to update signs`

## Localization
```json
{
  "Chat": "<color=#bebebe>[<color=#de8732>Discord Sign Logger</color>] {0}</color>",
  "NoPermission": "You do not have permission to perform this action",
  "BlockedMessage": "You're not allowed to update this sign/firework because you have been blocked. Your block will expire in {0}.",
  "KickReason": "Inappropriate sign/firework image",
  "BanReason": "Inappropriate sign/firework image"
}
```