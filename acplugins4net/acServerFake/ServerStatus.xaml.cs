using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace acServerFake
{
    /// <summary>
    /// Interaktionslogik für ServerStatus.xaml
    /// </summary>
    public partial class ServerStatus : UserControl
    {
        public ServerStatus()
        {
            InitializeComponent();
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            // Quickhack to close the UDP port upon shutdown - you would want to solve this via an behaviour
            var vm = this.DataContext as viemodel.ServerViewModel;
            if (vm != null)
                vm.CloseUDPConnection();
        }
    }
}
