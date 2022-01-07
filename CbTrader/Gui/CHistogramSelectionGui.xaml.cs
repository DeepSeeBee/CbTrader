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
    /// Interaktionslogik für CHistogramSelectionGui.xaml
    /// </summary>
    public partial class CHistogramSelectionGui : UserControl
    {
        public CHistogramSelectionGui()
        {
            InitializeComponent();
        }

        internal CSelection Selection { get => this.DataContext as CSelection; set => this.DataContext = value; }

        private void OnZoomButtonClicked(object sender, RoutedEventArgs e)
        {
            new Action(delegate () { this.Selection.ZoomIn(); }).InvokeWithExceptionMessageBox();
        }

        private void OnRequestDataButtonClicked(object sender, RoutedEventArgs e)
        {
            new Action(delegate () 
            {
                var aSelection = this.Selection;
                var aTrader = aSelection.Trader;
                var aProgressDialog = new CProgressDialog();
                aProgressDialog.Owner = this.GetVisualParent<Window>();
                aProgressDialog.Run(delegate ()
                {
                    aTrader.CompletePeriods(aProgressDialog, aTrader.InvestmentExchangeRateHistogram.DateRange);
                });
                var aZoom = false;
                aSelection.ExchangeRateHistogramGui.Select(aSelection, aZoom);
            }).InvokeWithExceptionMessageBox();
        }

        private void OnShowAllButtonClicked(object sender, RoutedEventArgs e)
        {
            new Action(delegate () { this.Selection.Reset(); }).InvokeWithExceptionMessageBox();
        }

        private void OnTruncateButtonClick(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                var aMsg = "This will permanently delete the selected data.";
                var aResponse = MessageBoxResult.OK == System.Windows.MessageBox
                    .Show(aMsg, "CbTrader", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (aResponse)
                {
                    this.Selection.Truncate();
                }
            }).InvokeWithExceptionMessageBox();


        }
    }
}
