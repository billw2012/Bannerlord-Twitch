using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BannerlordTwitch.Api
{
    class BLTApiSocketMessage
    {
        public SocketMessageType messageType;
        public string message;

        public BLTApiSocketMessage(SocketMessageType messageType, string message)
        {
            this.messageType = messageType;
            this.message = message;
        }
    }
}
