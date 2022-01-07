using CbTrader.Weight;
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
using CbTrader.Utils;

namespace CbTrader.Gui
{
    /// <summary>
    /// Interaktionslogik für CWeightTablesGui.xaml
    /// </summary>
    public partial class CWeightTablesGui : UserControl
    {
        public CWeightTablesGui()
        {
            InitializeComponent();
        }

        internal CWeightTablesVm WeightTablesVm => this.DataContext as CWeightTablesVm;

        private void OnAddTable(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                this.WeightTablesVm.Add();
            }).InvokeWithExceptionMessageBox();
        }

        private void OnRemoveTable(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                this.WeightTablesVm.Remove();
            }).InvokeWithExceptionMessageBox();
        }

        private void OnLoadDefaults(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                this.WeightTablesVm.LoadDefaults();
            }).InvokeWithExceptionMessageBox();

        }
    }
}
