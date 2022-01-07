using CbTrader.Settings;
using CbTrader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CbTrader.Gui
{
    /// <summary>
    /// Interaktionslogik für CInvestmentPlanGui.xaml
    /// </summary>
    public partial class CSettingsDialog : Window
    {
        public CSettingsDialog()
        {
            InitializeComponent();
        }

        private void OnCloseDialogRequested(object sender, EventArgs e)
        {
            this.Close();
        }

        internal CSettingsVm SettingsVm
        {
            get => this.DataContext as CSettingsVm;
            set => this.DataContext = value;
        }

        private void OnOpenSettingsDirectoryClick(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                var aFolderBrowserDialog = new FolderBrowserDialog();
                var aSettingsVm = this.SettingsVm;
                var aSettings = aSettingsVm.Settings;
                var aDirectoryInfo = aSettings.RootSettingsDirectory;
                aDirectoryInfo.Create();
                aFolderBrowserDialog.SelectedPath = aDirectoryInfo.FullName;
                aFolderBrowserDialog.Description = "Select directory to store data in (Note: Existing data will not be moved).";
                aFolderBrowserDialog.ShowNewFolderButton = true;
                var aDialogResult = aFolderBrowserDialog.ShowDialog();
                if(aDialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    var aNewDirectoryInfo = new DirectoryInfo(aFolderBrowserDialog.SelectedPath);
                    aSettingsVm.SettingsDirectoryInfo = aNewDirectoryInfo;
                }
            }).InvokeWithExceptionMessageBox();
        }

        private void OnTakeEffectButtonClicked(object sender, EventArgs e)
        {
            new Action(delegate ()
            {
                this.SettingsVm.TakeEffect();
            }).InvokeWithExceptionMessageBox();
        }

        private void OnOkButtonClicked(object sender, EventArgs e)
        {
            new Action(delegate ()
            {
                this.SettingsVm.TakeEffect();
                this.Close();
            }).InvokeWithExceptionMessageBox();
        }

        private void OnGetCoinApiKeyHyperlinkClicked(object sender, RoutedEventArgs e)
        {
            new Action(delegate ()
            {
                var aUrl = "https://www.coinapi.io/pricing?apikey";
                var aMsg = "CoinApi is used to request exchange rates from the internet." 
                            + Environment.NewLine + Environment.NewLine
                            + "Get a free CoinApi Key from: " 
                            + Environment.NewLine + Environment.NewLine 
                            + aUrl 
                            + Environment.NewLine + Environment.NewLine 
                            + "Copy URL to clipboard?";
                var aOk = MessageBoxResult.Yes == System.Windows.MessageBox.Show(aMsg, "CbTrader", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.Yes);
                if(aOk)
                {
                    System.Windows.Clipboard.SetText(aUrl);
                }
                //var aWebBrowserWindow = new CWebBrowserWindow();
                //aWebBrowserWindow.Owner = this.GetVisualParent<Window>();
                //aWebBrowserWindow.WebBrowser.Navigate(new Uri());
                //aWebBrowserWindow.ShowDialog();
            }).InvokeWithExceptionMessageBox();
        }
    }
}
