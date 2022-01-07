using CbTrader.InfoProviders;
using CbTrader.Settings;
using CbTrader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CbTrader.Tracker
{
    internal class CTracker : CViewModel
    {
        public CTracker()
        {
            var aStartedEvent = new AutoResetEvent(false);
            this.Task = new Task(delegate ()
            {
                this.DispatcherTimer.Start();
                this.DispatcherTimer.IsEnabled = true;
                this.DispatcherFrame = new DispatcherFrame();
                Dispatcher.PushFrame(this.DispatcherFrame);
                System.Diagnostics.Debug.Print(this.GetType().Name + ": Task finished.");
            });
            this.Task.Start();
            while(!(this.DispatcherFrame is object))
                Thread.Sleep(1);
            Thread.Sleep(10);
        }
        protected DispatcherFrame DispatcherFrame;
        protected override void OnDispose()
        {
            base.OnDispose();
            this.DispatcherFrame.Continue = false;
            this.Task.Wait();
        }

        private Task Task;

        #region InfoProvider
        private ICurrencyInfoProvider InfoProviderM;
        private ICurrencyInfoProvider InfoProvider => CLazyLoad.Get(ref this.InfoProviderM, () => throw new NotImplementedException());
        #endregion

        private TimeSpan Intervall => TimeSpan.FromMinutes(24d * 60d / (double)this.InfoProvider.RequestsPerDay);

        private DispatcherTimer DispatcherTimerM;
        private DispatcherTimer DispatcherTimer => CLazyLoad.Get(ref this.DispatcherTimerM, this.NewDispatcherTimer);
        private DispatcherTimer NewDispatcherTimer()
        {
            var aTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, new EventHandler(this.OnTimer), Dispatcher.CurrentDispatcher);
            return aTimer;
        }

        private bool ActiveM;
        internal bool Active
        {
            get => this.ActiveM;
            set
            {
                this.ActiveM = value;
                this.OnPropertyChanged(nameof(this.VmActive));
            }
        }
        public bool VmActive { get => this.Active; set => this.Active = value; }
        private CSettings Settings => throw new NotImplementedException();
        private CCurrency FromCurrency => this.Settings.SellCurrency;
        private CCurrency ToCurrency => this.Settings.BuyCurrency;

        private DateTime? LastExecutionTime;
        private void OnTimer(object aSender, EventArgs aArgs)
        {
            bool aExcute;
            if(!this.Active)
            {
                aExcute = false;
            }
            else if(this.LastExecutionTime.HasValue)
            {
                var aPassed = DateTime.Now.Subtract(this.LastExecutionTime.Value);
                aExcute = aPassed.CompareTo(this.Intervall) > 0;
            }
            else
            {
                aExcute = true;
            }
            if(aExcute)
            {
                var aFromCurrency = this.FromCurrency;
                var aToCurrency = this.ToCurrency;
                var aExchangeRate = this.InfoProvider.GetCurrentExchangeRate(aFromCurrency, aToCurrency);
                var aXml = aExchangeRate.ToXmlString();
                var aFileInfo = this.Settings.GetExchangeRateTrackerFileInfo();
                aFileInfo.Directory.Create();
                File.AppendAllLines(aFileInfo.FullName, new string[] { aXml });
                this.LastExecutionTime = DateTime.Now;
            }
        }
    }
}
