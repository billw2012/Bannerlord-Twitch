using Newtonsoft.Json.Linq;

namespace BannerlordTwitch
{
    public interface IBotCommand
    {
        void Execute(string args, string userName, string replyId, JObject config);
    }
}