using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Ext.Discord.Entities;
using Rust.SignLogger.Ids;

namespace Rust.SignLogger.Configuration;

public class ImageButton
{
    [JsonProperty(PropertyName = "Button ID")]
    public ButtonId ButtonId { get; set; }
        
    [JsonProperty(PropertyName = "Button Display Name")]
    public string DisplayName { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty(PropertyName = "Button Style")]
    public ButtonStyle Style { get; set; }
    
    [JsonProperty(PropertyName = "Commands")]
    public List<string> Commands { get; set; }
    
    [JsonProperty(PropertyName = "Player Message")]
    public string PlayerMessage { get; set; }

    [JsonProperty(PropertyName = "Server Message")]
    public string ServerMessage { get; set; }
    
    [JsonProperty(PropertyName = "Show Confirmation Modal")]
    public bool ConfirmModal { get; set; }
        
    [JsonProperty(PropertyName = "Requires Permissions To Use Button")]
    public bool RequirePermissions { get; set; }
        
    [JsonProperty(PropertyName = "Allowed Discord Roles (Role ID)")]
    public List<Snowflake> AllowedRoles { get; set; }
        
    [JsonProperty(PropertyName = "Allowed Oxide Groups (Group Name)")]
    public List<string> AllowedGroups { get; set; }

    [JsonConstructor]
    public ImageButton() { }
        
    public ImageButton(ImageButton settings)
    {
        ButtonId = settings.ButtonId;
        DisplayName = settings.DisplayName ?? "Button Display Name";
        Style = settings.Style;
        Commands = settings.Commands ?? new List<string>();
        PlayerMessage = settings.PlayerMessage ?? string.Empty;
        ServerMessage = settings.ServerMessage ?? string.Empty;
        ConfirmModal = settings.ConfirmModal;
        RequirePermissions = settings.RequirePermissions;
        AllowedRoles = settings.AllowedRoles ?? new List<Snowflake>();
        AllowedGroups = settings.AllowedGroups ?? new List<string>();
    }
}