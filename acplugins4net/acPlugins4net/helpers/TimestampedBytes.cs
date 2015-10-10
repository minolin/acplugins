using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace acPlugins4net.helpers
{
    public class TimestampedBytes
    {
        public byte[] RawData = null;
        public DateTime IncomingDate = DateTime.Now;

        public TimestampedBytes(byte[] rawData)
        {
            RawData = rawData;
        }
    }
}
