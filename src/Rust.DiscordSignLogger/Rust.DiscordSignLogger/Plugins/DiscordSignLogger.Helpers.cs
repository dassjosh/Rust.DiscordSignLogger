using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=9
public partial class DiscordSignLogger
{
    public IPlayer FindPlayerById(string id) => covalence.Players.FindPlayerById(id);

    public void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _pluginData);

    public void Puts(string format) => base.Puts(format);
}