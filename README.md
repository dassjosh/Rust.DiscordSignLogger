## Features

* Image Logging for Signage / Photo Frames / Fireworks / Carvable Pumpkins / Neon Signs / Sign Artist Updates to Discord
* Customizable Discord Buttons to take action against the entity / player that updated the sign
    * Buttons Can Locked Behind Discord Roles / Oxide Groups (Oxide Groups Requires a Discord Link Plugin)
    * Logging of which user pressed a button and the command that was run
    * Server / Player messages when a button is pressed
* Permanent / Temporary Image Update Bans
* Erasing Sign Image / Replacing Sign Image With Configurable Image or Text
* Fully Customizable Discord Embed Message

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
  "Action Log Settings": {
    "Action Log Channel ID": "",
    "Action Log Buttons": [
      {
        "Button Display Name": "Image Message",
        "Button Style": "Link",
        "Commands": [
          "discord://-/channels/{dsl.action.guild.id}/{dsl.action.channel.id}/{dsl.action.message.id}"
        ]
      }
    ]
  },
  "Disable Discord Button After User": true,
  "Delete Saved Log Data After (Days)": 14.0,
  "Delete Cached Button Data After (Days)": 14.0,
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
      "Discord Channel ID": "",
      "Message Config": {
        "content": "",
        "embeds": [
          {
            "Title": "{server.name}",
            "Description": "",
            "Url": "",
            "Embed Color (Hex Color Code)": "#AC7061",
            "Image Url": "attachment://image.png",
            "Thumbnail Url": "",
            "Add Timestamp": true,
            "Embed Fields": [
              {
                "Title": "Player:",
                "Value": "{player.name} ([{player.id}](https://steamcommunity.com/profiles/{player.id}))",
                "Inline": true
              },
              {
                "Title": "Owner:",
                "Value": "{dsl.entity.owner.name} ([{dsl.entity.owner.id}](https://steamcommunity.com/profiles/{dsl.entity.owner.id}))",
                "Inline": true
              },
              {
                "Title": "Position:",
                "Value": "{dsl.entity.position:0.00!x} {dsl.entity.position:0.00!y} {dsl.entity.position:0.00!z}",
                "Inline": true
              },
              {
                "Title": "Item:",
                "Value": "{dsl.entity.name}",
                "Inline": true
              },
              {
                "Title": "Texture Index:",
                "Value": "{dsl.entity.textureindex}",
                "Inline": true
              }
            ],
            "Footer": {
              "Icon Url": "",
              "Text": "",
              "Enabled": true
            }
          }
        ]
      },
      "Button Commands": [
        {
          "Player Message": "",
          "Server Message": "",
          "Requires Permissions To Use Button": false,
          "Allowed Discord Roles (Role ID)": [],
          "Allowed Oxide Groups (Group Name)": [],
          "Button Display Name": "Player Profile",
          "Button Style": "Link",
          "Commands": [
            "https://steamcommunity.com/profiles/{player.id}"
          ]
        },
        {
          "Player Message": "",
          "Server Message": "",
          "Requires Permissions To Use Button": false,
          "Allowed Discord Roles (Role ID)": [],
          "Allowed Oxide Groups (Group Name)": [],
          "Button Display Name": "Owner Profile",
          "Button Style": "Link",
          "Commands": [
            "https://steamcommunity.com/profiles/{dsl.entity.owner.id}"
          ]
        },
        {
          "Player Message": "An admin erased your sign for being inappropriate",
          "Server Message": "",
          "Requires Permissions To Use Button": false,
          "Allowed Discord Roles (Role ID)": [],
          "Allowed Oxide Groups (Group Name)": [],
          "Button Display Name": "Erase",
          "Button Style": "Primary",
          "Commands": [
            "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}"
          ]
        },
        {
          "Player Message": "An admin erased your sign for being inappropriate",
          "Server Message": "",
          "Requires Permissions To Use Button": true,
          "Allowed Discord Roles (Role ID)": [],
          "Allowed Oxide Groups (Group Name)": [],
          "Button Display Name": "Sign Block (24 Hours)",
          "Button Style": "Primary",
          "Commands": [
            "dsl.signblock {player.id} 1440.0"
          ]
        },
        {
          "Player Message": "An admin killed your sign for being inappropriate",
          "Server Message": "",
          "Requires Permissions To Use Button": true,
          "Allowed Discord Roles (Role ID)": [],
          "Allowed Oxide Groups (Group Name)": [],
          "Button Display Name": "Kill Entity",
          "Button Style": "Secondary",
          "Commands": [
            "entid kill {dsl.entity.id}"
          ]
        },
        {
          "Player Message": "",
          "Server Message": "",
          "Requires Permissions To Use Button": true,
          "Allowed Discord Roles (Role ID)": [],
          "Allowed Oxide Groups (Group Name)": [],
          "Button Display Name": "Kick Player",
          "Button Style": "Danger",
          "Commands": [
            "kick {player.id} \"{dsl.kick.reason}\"",
            "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}"
          ]
        },
        {
          "Player Message": "",
          "Server Message": "",
          "Requires Permissions To Use Button": true,
          "Allowed Discord Roles (Role ID)": [],
          "Allowed Oxide Groups (Group Name)": [],
          "Button Display Name": "Ban Player",
          "Button Style": "Danger",
          "Commands": [
            "ban {player.id} \"{dsl.ban.reason}\"",
            "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}"
          ]
        }
      ]
    }
  ],
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

## Placeholders
This plugin supports all default PlaceholderApi placeholders and adds some additional ones listed below.

`{dsl.entity.id}` - Returns the entity ID  
`{dsl.entity.textureindex}` - Returns the texture index for the image  
`{dsl.entity.name}` - Returns the entity item name  
`{dsl.entity.owner.id}` - Returns the entity owner steam id  
`{dsl.entity.owner.name}` - Returns the entity owner player name  
`{dsl.entity.position}` - Returns the entity position on the server  
`{dsl.signartist.url}` - Returns the sign artist url  
`{dsl.discord.user.id}` - Returns the discord user ID of the user who clicked on the button  
`{dsl.discord.user.name}` - Returns the discord user name of the user who clicked on the button  
`{dsl.kick.reason}` - Returns the kick reason lang message  
`{dsl.ban.reason}` - Returns the ban reason lang message  
`{dsl.action.guild.id}` - Actioned Message Guild ID  
`{dsl.action.channel.id}` - Actioned Message Channel ID  
`{dsl.action.message.id}` - Actioned Message Message ID

## Commands

`dsl.erase {entityId} {textureIndex}` - Will erase the image from the entity id with the given index  
`dsl.signblock {playerId} {durationInSeconds}`  - Will block a player from updating a sign for specified minutes. If no time is specified block will be permanent.  
`dsl.signunblock {playerId}` - Will unblock a previous blocked player. Allowing them to update signs again.

## Localization
```json
{
  "Chat": "<color=#bebebe>[<color=#de8732>Discord Sign Logger</color>] {0}</color>",
  "NoPermission": "You do not have permission to perform this action",
  "KickReason": "You have been kicked for an inappropriate sign/firework image",
  "BanReason": "You have been banned for an inappropriate sign/firework image",
  "SignBannedMessage": "You're not allowed to update this sign because you have been banned. Your ban will expire in {0}.",
  "FireworkBannedMessage": "You're not allowed to update this firework because you have been banned. Your ban will expire in {0}.",
  "ActionMessage": "[Discord Sign Logger] <@{dsl.discord.user.id}> ran command \"{dsl.command}\"",
  "DeletedLog": "The log data for this message was not found. If it's older than {0} days then it may have been deleted.",
  "DeletedButtonCache": "Button was not found in cache. If this message is older than {0} days then it may have been deleted.",
  "Format.Day": "day ",
  "Format.Days": "days ",
  "Format.Hour": "hour ",
  "Format.Hours": "hours ",
  "Format.Minute": "minute ",
  "Format.Minutes": "minutes ",
  "Format.Second": "second",
  "Format.Seconds": "seconds",
  "Format.TimeField": "<color=#de8732>{0}</color> {1}",
  "SignArtistTitle": "Sign Artist URL:",
  "SignArtistValue": "[URL]({dsl.signartist.url})"
}
```