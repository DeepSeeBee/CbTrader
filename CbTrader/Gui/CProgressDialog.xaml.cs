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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CbTrader.Gui
{
    /// <summary>
    /// Interaktionslogik für CProgressWindow.xaml
    /// </summary>
    public partial class CProgressDialog : Window, IProgress
    {
        public CProgressDialog()
        {
            InitializeComponent();
        }
        public string ProgressTitle 
        { 
            get => this.TitleTextBox.Text; 
            set => this.Dispatcher.Invoke(new Action(delegate () { this.TitleTextBox.Text = value; })); 
        }
        public string ProgressSubTitle 
        { 
            get => this.SubTitleTextBox.Text; 
            set => this.Dispatcher.Invoke(new Action(delegate () { this.SubTitleTextBox.Text = value; }));  
        }
        public double ProgressPercent 
        { 
            get => this.ProgressBar.Value; 
            set => this.Dispatcher.Invoke(new Action(delegate () { this.ProgressBar.Value = value; }));
        }
        private volatile bool CancelationPending;
        private readonly DispatcherFrame DispatcherFrame = new DispatcherFrame();
        public void CheckCancel()
        {
            if (this.CancelationPending)
            {
                throw new OperationCanceledException();
            }
        }
        public void Run( Action  aAction)
        {
            if (this.DispatcherFrame.Continue)
            {
                var aFrame = this.DispatcherFrame;
                var aTask = Task.Run(delegate ()
                {
                    try
                    {
                        aAction();
                    }
                    finally
                    {
                        aFrame.Continue = false;
                    }
                });
                this.Dispatcher.BeginInvoke(new Action(delegate
                {
                    System.Windows.Threading.Dispatcher.PushFrame(this.DispatcherFrame);
                    this.Close();
                }));
                this.ShowDialog();
                aTask.GetAwaiter().GetResult();
            }
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.CancelButton.IsEnabled = false;
            this.CancelationPending = true;
        }
    }
}
