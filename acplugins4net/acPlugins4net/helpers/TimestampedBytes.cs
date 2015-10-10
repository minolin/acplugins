using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace acPlugins4net.helpers
{
    public class TimestampedBytes
    {
        public byte[] RawData;
        public DateTime IncomingDate;

        public TimestampedBytes(byte[] rawData)
        {
            RawData = rawData;
            IncomingDate = DateTime.Now;
        }

        public TimestampedBytes(byte[] rawData, DateTime incomingDate)
        {
            RawData = rawData;
            IncomingDate = incomingDate;
        }
    }
}
