using Newtonsoft.Json.Linq;

namespace BannerlordTwitch
{
    public class CommandMessage
    {
        public string UserName;
        public string ReplyId;
        public int Bits;
        public double BitsInDollars;
        public int SubscribedMonthCount;
        public bool IsBroadcaster;
        public bool IsHighlighted;
        public bool IsMe;
        public bool IsModerator;
        public bool IsSkippingSubMode;
        public bool IsSubscriber;
        public bool IsVip;
        public bool IsStaff;
        public bool IsPartner;
    }
    
    public interface IBotCommand
    {
        void Execute(string args, CommandMessage message, JObject config);
    }
}