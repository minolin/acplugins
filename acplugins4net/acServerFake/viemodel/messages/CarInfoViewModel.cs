using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel.messages
{
    public class CarInfoViewModel : BaseMessageViewModel<MsgCarInfo>
    {
        public override string MsgCaption { get { return "Car Info"; } }

        public CarInfoViewModel()
        {
        }

        public override string ToString()
        {
            return "CarId=" + Message.CarId + "|Model=" + Message.CarModel + "|DriverName=" + Message.DriverName + "|Guid=" + Message.DriverGuid;
        }
    }
}
