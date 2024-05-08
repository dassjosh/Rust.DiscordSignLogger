namespace Rust.SignLogger.Plugins;

//Define:FileOrder=13
public partial class DiscordSignLogger
{
    public string GetEntityName(BaseEntity entity)
    {
        if (!entity.IsValid())
        {
            return string.Empty;
        }

        if (_prefabNameCache.TryGetValue(entity.prefabID, out string name))
        {
            return name;
        }
            
        if (RustTranslationAPI is { IsLoaded: true })
        {
            name = RustTranslationAPI.Call<string>("GetDeployableTranslation", lang.GetServerLanguage(), entity.ShortPrefabName);
            if (!string.IsNullOrEmpty(name))
            {
                _prefabNameCache[entity.prefabID] = name;
                return name;
            }
        }
        
        _prefabNameCache[entity.prefabID] = entity.ShortPrefabName;
        return entity.ShortPrefabName;
    }
        
    public string GetItemName(int itemId)
    {
        if (itemId == 0)
        {
            return string.Empty;
        }

        if (_itemNameCache.TryGetValue(itemId, out string name))
        {
            return name;
        }
            
        if (RustTranslationAPI is { IsLoaded: true })
        {
            name = RustTranslationAPI.Call<string>("GetItemTranslationByID", lang.GetServerLanguage(), itemId);
            if (!string.IsNullOrEmpty(name))
            {
                _itemNameCache[itemId] = name;
                return name;
            }
        }

        name = ItemManager.FindItemDefinition(itemId).displayName.translated;
        _itemNameCache[itemId] = name;
        return name;
    }
}