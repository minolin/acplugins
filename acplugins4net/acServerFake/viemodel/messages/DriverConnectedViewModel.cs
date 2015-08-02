using acPlugins4net.messages;
using acServerFake.viemodel.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel.messages
{
    public class DriverConnectedViewModel : BaseMessageViewModel<MsgNewConnection>
    {
        public override string MsgCaption
        {
            get
            {
                return "New driver";
            }
        }

        [Obsolete("nicht fertig")]
        public DriverConnectedViewModel()
        {
            Message.CarId = 7;
            Message.CarModel = "ks_porsche_997_cup";
            Message.CarSkin = "flying_horse_2015";
            Message.DriverGuid = "96561198021090313"; // sorry I can't plug my real one, otherwise I have the feeling to be blacklisted everywhere
            Message.DriverName = "Minolin";

            // TODO: Parsing an Entry_List to have a predefined answer to each car_info request.
            // That will help big times because it'll be consistent
        }
    }
}
