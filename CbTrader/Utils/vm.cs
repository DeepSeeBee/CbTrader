using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbTrader.Utils
{
    public abstract partial class CViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        internal virtual void OnPropertyChanged(string aPropertyName)
        {
            if (this.PropertyChanged is object)
            {
                var aArgs = new PropertyChangedEventArgs(aPropertyName);
                try
                {
                    this.PropertyChanged(this, aArgs);
                }
                catch(Exception )
                {
                }
            }
        }
        protected virtual void OnDispose() { }
        internal void Dispose()
        {
            this.OnDispose();
        }

        protected void Set<T>(ref T aVar, T aNewValue, params string[] aPropertyNames)
        {
            aVar = aNewValue;
            foreach (var aPropertyName in aPropertyNames)
            {
                this.OnPropertyChanged(aPropertyName);
            }
        }
    }

}
