using acPlugins4net.messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace acServerFake.viemodel.messages
{
    public class CarInfoCollection
    {
        public ObservableCollection<CarInfoViewModel> Cars { get; set; }
        public RelayCommand UpdateEntryList { get; set; }

        public string MsgCaption { get { return "CarInfo configuration"; } }

        public CarInfoCollection()
        {
            Cars = new ObservableCollection<CarInfoViewModel>();
            UpdateEntryList = new RelayCommand("Refresh", (p) =>
            {
                ReloadEntryList();
            });
            UpdateEntryList.Execute(null);
        }

        private void ReloadEntryList()
        {
            // We'll expect a entry_list.ini where the car definitions are stored (you can use a real one)
            Cars.Clear();

            if (!File.Exists(@"cfg\entry_list.ini") && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
                throw new Exception("We'd expect a cfg/entry_list.ini next to the acServerFake.exe. You can use a real one.");

            // Now some ugly ini-parsing, but I didn't want to add a whole lib as dependancy
            CarInfoViewModel currentCarInfo = null;
            foreach (var line in File.ReadAllLines(@"cfg\entry_list.ini"))
            {
                if (line.StartsWith("[CAR_"))
                {
                    currentCarInfo = new CarInfoViewModel();
                    currentCarInfo.Message.CarId = Convert.ToByte(line.Replace("[CAR_", "").Replace("]", ""));
                    Cars.Add(currentCarInfo);
                }
                else if (line.StartsWith("MODEL="))
                    currentCarInfo.Message.CarModel = line.Replace(("MODEL="), "");
                else if (line.StartsWith("SKIN="))
                    currentCarInfo.Message.CarSkin = line.Replace(("SKIN="), "");
                else if (line.StartsWith("DRIVERNAME="))
                {
                    currentCarInfo.Message.DriverName = line.Replace(("DRIVERNAME="), "");
                    if (!string.IsNullOrEmpty(currentCarInfo.Message.DriverName))
                        currentCarInfo.Message.IsConnected = true;
                }
                else if (line.StartsWith("TEAM="))
                    currentCarInfo.Message.DriverTeam = line.Replace(("TEAM="), "");
                else if (line.StartsWith("GUID="))
                {
                    currentCarInfo.Message.DriverGuid = line.Replace(("GUID="), "");
                    if (!string.IsNullOrEmpty(currentCarInfo.Message.DriverName))
                        currentCarInfo.Message.IsConnected = true;
                }
            }
        }

        internal MsgCarInfo GetMessage(byte carId)
        {
            foreach(var info in Cars)
            {
                if (info.Message.CarId == carId)
                    return info.Message;
            }

            throw new Exception("No car with Id " + carId + " configured (in entry_list.ini)");
        }
    }
}
