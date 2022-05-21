using Newtonsoft.Json;

namespace Rust.SignLogger.Configuration.ActionLog
{
    public class ActionMessageButtonCommand : BaseDiscordButton
    {
        [JsonConstructor]
        public ActionMessageButtonCommand()
        {
            
        }

        public ActionMessageButtonCommand(ActionMessageButtonCommand settings) : base(settings)
        {

        }
    }
}