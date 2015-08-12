using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace acPlugins4net.messages
{
    public class MsgNewSession : MsgSessionInfo
    {
        public MsgNewSession()
        {
            Type = kunos.ACSProtocol.MessageType.ACSP_NEW_SESSION;
        }
    }
}
