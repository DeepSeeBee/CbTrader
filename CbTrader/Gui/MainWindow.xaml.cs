using CbTrader.Utils;
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

namespace CbTrader.Gui
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this.CbTrader;
        }

        private CCbTrader CbTrader = new CCbTrader();

        private void OnTestButtonClick(object sender, RoutedEventArgs e)
        {
            new Action(CTests.Run).InvokeWithExceptionMessageBox();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            this.CbTrader.Dispose();
        }

        private void OnCompletePeriodsButtonClick(object sender, RoutedEventArgs e)
        {
            new Action(delegate()
            {
                var aProgressDialog = new CProgressDialog();
                aProgressDialog.Owner = this;
                aProgressDialog.Run(delegate ()
                {
                    this.CbTrader.CompletePeriods(aProgressDialog);
                });
            }).InvokeWithExceptionMessageBox();
        }

        private void OnShowSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                var aDialog = new CSettingsDialog();
                aDialog.Owner = this;
                aDialog.SettingsVm = this.CbTrader.SettingsVm;
                aDialog.ShowDialog();
            }).InvokeWithExceptionMessageBox();
        }

        private void OnPlaceOrdersClick(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                this.CbTrader.LimitOrdersVms = default;
            }).InvokeWithExceptionMessageBox();
        }

        private void OnShowAllButtonClicked(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                this.CbTrader.InvestmentExchangeRateHistogram = default;
            }).InvokeWithExceptionMessageBox();
        }

        private void ResetLimitOrders(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                this.CbTrader.LimitOrdersVms.RefreshWeights();
            }).InvokeWithExceptionMessageBox();

        }
    }
}
