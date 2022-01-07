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

    internal interface IDialog
    {
        void Close();
        void TakeEffect();
    }
    /// <summary>
    /// Interaktionslogik für CDialogButtons.xaml
    /// </summary>
    public partial class CDialogButtons : UserControl
    {
        public CDialogButtons()
        {
            InitializeComponent();
        }
        private IDialog Dialog => this.DataContext as IDialog;

        private void TakeEffect()
        {
            new Action(delegate ()
            {
                if (this.Dialog is object)
                {
                    this.Dialog.TakeEffect();
                    this.Dialog.Close();
                }
            }).InvokeWithExceptionMessageBox();
        }

        public event EventHandler OkButtonClicked;
        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            if(this.OkButtonClicked is object)
            {
                this.OkButtonClicked(this, new EventArgs());
            }
            this.TakeEffect();
        }

        public event EventHandler CancelButtonClicked;

        public event EventHandler CloseDialogRequested;
        private void OnCloseDialogRequested()
        {
            if(this.CloseDialogRequested is object)
            {
                this.CloseDialogRequested(this, new EventArgs());
            }
        }

        private void Close()
        {
            if (this.Dialog is object)
            {
                this.Dialog.Close();
            }
            this.OnCloseDialogRequested();
        }
        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.CancelButtonClicked is object)
            {
                this.CancelButtonClicked(this, new EventArgs());
            }
            new Action(this.Close).InvokeWithExceptionMessageBox();
        }

        public event EventHandler TakeEffectButtonClicked;
        private void OnTakeEffectButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.TakeEffectButtonClicked is object)
            {
                this.TakeEffectButtonClicked(this, new EventArgs());
            }
            new Action(delegate ()
            {
                if (this.Dialog is object)
                {
                    this.Dialog.TakeEffect();
                }
            }).InvokeWithExceptionMessageBox();
        }


        public static readonly DependencyProperty TakeEffectIsEnabledProperty = DependencyProperty.Register("TakeEffectIsEnabled", typeof(bool), typeof(CDialogButtons), new PropertyMetadata(false));
        public bool TakeEffectIsEnabled
        {
            get => (bool)this.GetValue(TakeEffectIsEnabledProperty);
            set => this.SetValue(TakeEffectIsEnabledProperty, value);
        }



        public static readonly DependencyProperty OkIsEnabledChangedProperty = DependencyProperty.Register("OkIsEnabled", typeof(bool), typeof(CDialogButtons), new PropertyMetadata(true));
        public bool OkIsEnabledChanged
        {
            get => (bool)this.GetValue(OkIsEnabledChangedProperty);
            set => this.SetValue(OkIsEnabledChangedProperty, value);
        }
    }
}
