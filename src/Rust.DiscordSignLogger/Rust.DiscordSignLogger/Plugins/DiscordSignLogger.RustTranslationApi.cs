namespace Rust.SignLogger.Plugins
{
    //Define:FileOrder=12
    public partial class DiscordSignLogger
    {
        public string GetEntityName(BaseEntity entity)
        {
            if (!entity)
            {
                return string.Empty;
            }
            
            if (RustTranslationAPI != null && RustTranslationAPI.IsLoaded)
            {
                string name = RustTranslationAPI.Call<string>("GetDeployableTranslation", lang.GetServerLanguage(), entity.ShortPrefabName);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return _prefabNameLookup[entity.ShortPrefabName] ?? entity.ShortPrefabName;
        }
        
        public string GetItemName(int itemId)
        {
            if (itemId == 0)
            {
                return string.Empty;
            }
            
            if (RustTranslationAPI != null && RustTranslationAPI.IsLoaded)
            {
                string name = RustTranslationAPI.Call<string>("GetItemTranslationByID", lang.GetServerLanguage(), itemId);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return ItemManager.FindItemDefinition(itemId).displayName.translated;
        }
    }
}