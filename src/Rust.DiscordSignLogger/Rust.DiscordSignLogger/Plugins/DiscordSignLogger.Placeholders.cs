using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Cache;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Ids;
using Rust.SignLogger.Placeholders;
using Rust.SignLogger.State;
using UnityEngine;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=11
public partial class DiscordSignLogger
{
    public void RegisterPlaceholders()
    {
        _placeholders.RegisterPlaceholder<SignUpdateState, ulong>(this, PlaceholderKeys.EntityId, PlaceholderDataKeys.State, state => state.EntityId);
        _placeholders.RegisterPlaceholder<SignUpdateState, string>(this, PlaceholderKeys.EntityName, PlaceholderDataKeys.State, state => GetEntityName(state.Entity));
        _placeholders.RegisterPlaceholder<SignUpdateState, string>(this, PlaceholderKeys.ItemName, PlaceholderDataKeys.State, state => GetItemName(state.ItemId));
        _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.PlayerMessage, PlaceholderDataKeys.PlayerMessage);
        _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.ServerMessage, PlaceholderDataKeys.ServerMessage);
        _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.SignArtistUrl, PlaceholderDataKeys.SignArtistUrl);
        _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.Command, PlaceholderDataKeys.Command);
        _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.ButtonId, PlaceholderDataKeys.ButtonId);
        _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.PlayerId, PlaceholderDataKeys.PlayerId);
        _placeholders.RegisterPlaceholder<TemplateKey>(this, PlaceholderKeys.MessageId, PlaceholderDataKeys.MessageId);
        _placeholders.RegisterPlaceholder<StateKey, string>(this, PlaceholderKeys.MessageState, PlaceholderDataKeys.MessageState, state => state.State);
        _placeholders.RegisterPlaceholder<SignUpdateState, string>(this, PlaceholderKeys.TextureIndex, PlaceholderDataKeys.State, state =>
        {
            if (state.Entity is ISignage signage && signage.GetTextureCRCs().Length <= 1)
            {
                return null;
            }
            
            return StringCache<byte>.Instance.ToString(state.TextureIndex);
        });
        _placeholders.RegisterPlaceholder<SignUpdateState, GenericPosition>(this, PlaceholderKeys.Position, PlaceholderDataKeys.State, state =>
        {
            BaseEntity entity = state.Entity;
            Vector3 pos = entity ? entity.transform.position : Vector3.zero;
            return new GenericPosition(pos.x, pos.y, pos.z);
        });
        
        PlayerPlaceholders.RegisterPlaceholders(this, PlaceholderKeys.OwnerKeys, PlaceholderDataKeys.Owner);
    }
    
    public PlaceholderData GetPlaceholderData(SignUpdateState state, DiscordInteraction interaction) => GetPlaceholderData(state).AddInteraction(interaction);

    public PlaceholderData GetPlaceholderData(SignUpdateState state)
    {
        return GetPlaceholderData()
            .AddPlayer(state.Player)
            .Add(PlaceholderDataKeys.State, state)
            .Add(PlaceholderDataKeys.Owner, state.Owner);
    }

    public PlaceholderData GetPlaceholderData(DiscordInteraction interaction) => GetPlaceholderData().AddInteraction(interaction);
    
    public PlaceholderData GetPlaceholderData()
    {
        return _placeholders.CreateData(this);
    }
}