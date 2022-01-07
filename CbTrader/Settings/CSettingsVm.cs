using CbTrader.Domain;
using CbTrader.InfoProviders;
using CbTrader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CbTrader.Settings
{
    using CInterpolateSetting = Tuple<bool, TimeSpan>;

    [XmlRoot("Settings")]
    public sealed class CSettingsVm : CViewModel
    {
        public CSettingsVm()
        {
            this.LoadDefaults();
            this.SetChanged(false);
        }
        internal CSettingsVm(CCbTrader aTrader):this()
        {
            this.Trader = aTrader;

        }
        internal readonly CCbTrader Trader;

        internal CInvestmentPlan InvestmentPlan 
        { 
            get => this.Trader.InvestmentPlan;
            set => this.Trader.InvestmentPlan = value;
        }

        private void Save()
        {
            var aSerializer = new XmlSerializer(this.GetType());
            var aStream = new MemoryStream();
            aSerializer.Serialize(aStream, this);
            aStream.Seek(0, SeekOrigin.Begin);
            using (var aFileStream = this.Settings.SettingsFileInfo.OpenWrite())
            {
                aStream.CopyTo(aFileStream);
                aFileStream.SetLength(aFileStream.Position);
            }
        }

        private void OnChange()
        {
            this.Changed = true;
        }

        private bool ChangedM;
        internal bool Changed
        {
            get => this.ChangedM;
            set
            {
                this.ChangedM = value;
                this.OnPropertyChanged(nameof(this.VmChanged));
            }
        }

        internal void ShowSettingsDirectoryChooserDialog()
        {
            throw new NotImplementedException();
        }
        internal void Assign(CSettingsVm rhs)
        {
            this.LookBackTimeSpan = rhs.LookBackTimeSpan;
            this.PeriodEnum = rhs.PeriodEnum;
            this.TradeCount = rhs.TradeCount;
            this.ProfitMinFaktor = rhs.ProfitMinFaktor;
            this.ProfitTargetFaktor = rhs.ProfitTargetFaktor;
            this.InvestAmount = rhs.InvestAmount;
            this.InterpolateSetting = rhs.InterpolateSetting;
            this.CoinApiKey = rhs.CoinApiKey;
        }
        [XmlIgnore]
        public bool VmChanged
        {
            get => this.Changed;
            set => this.Changed = value;
        }
        private FileInfo SettingsFileInfo => this.Settings.SettingsFileInfo;
        internal void LoadFromPersistentStorage(bool aInitialLoad = false)
        {
            var aSettingsFile = this.SettingsFileInfo;
            aSettingsFile.Refresh();
            if(aSettingsFile.Exists)
            {
                var aSerializer = new XmlSerializer(this.GetType());
                CSettingsVm aSettingsVm;
                using (var aStream = this.Settings.SettingsFileInfo.OpenRead())
                {
                    aSettingsVm = (CSettingsVm) aSerializer.Deserialize(aStream);
                }
                this.Assign(aSettingsVm);
                this.TakeEffect();
            }
            else
            {
                this.LoadDefaults();
            }
            if (aInitialLoad)
            {
                this.SetChanged(false);
            }
            else
            {
                this.TakeEffect();
            }
        }
        private void LoadDefaults()
        {
            this.PeriodEnum = CPeriodEnum.Hours1;
            this.LookBackTimeSpan = TimeSpan.FromDays(7);
            this.TradeCount = 9;
            this.SettingsDirectoryInfo = this.Trader is object ? this.Trader.Settings.SettingsDirectoryInfo : CSettings.RootSettingsDirectoryStatic;
            this.ProfitMinFaktor = this.ProfitMinFaktorDefault;
            this.ProfitTargetFaktor = this.ProfitTargetFaktorDefault;
            this.InterpolateSetting = this.InterpolateSettingDefault;
            this.CoinApiKey = string.Empty;
        }

        private bool PeriodEnumChanged;
        private CPeriodEnum PeriodEnumM;
        internal CPeriodEnum PeriodEnum 
        {
            get => this.PeriodEnumM;
            set
            {
                if (!object.Equals(value, this.PeriodEnumM))
                {
                    this.PeriodEnumM = value;
                    this.PeriodEnumChanged = true;
                    this.OnChange();
                    this.OnPropertyChanged(nameof(this.VmPeriodEnum));
                }
            }
        }

        public IEnumerable<CPeriodEnum> VmPeriodEnums => Enum.GetValues(typeof(CPeriodEnum)).Cast<CPeriodEnum>();
        
        [XmlElement("Period")]
        public CPeriodEnum VmPeriodEnum
        {
            get => this.PeriodEnum;
            set => this.PeriodEnum = value;
        }

        [XmlElement("LookBackTimeSpanDays")]
        public string VmLookBackTimeSpanText
        {
            get
            {
                return this.LookBackTimeSpan.TotalDays.ToString(); //.ToString("D");
            }
            set
            {
                this.LookBackTimeSpan = TimeSpan.FromDays(double.Parse(value));
            }
        }

        private TimeSpan LookBackTimeSpanM;
        internal TimeSpan LookBackTimeSpan 
        {
            get => this.LookBackTimeSpanM;
            set
            {
                this.LookBackTimeSpanM = value;
                this.OnChange();
                this.OnPropertyChanged(nameof(this.VmLookBackTimeSpanText));
            }
        }

        [XmlElement("TradeCount")]
        public string VmTradeCountText
        {
            get => this.TradeCount.ToString();
            set => this.TradeCount = int.Parse(value);
        }
        private bool TradeCountChanged;
        internal int TradeCountM;
        internal int TradeCount
        {
            get => this.TradeCountM;
            set
            {
                if (!object.Equals(this.TradeCountM, value))
                {
                    this.TradeCountM = value;
                    this.TradeCountChanged = true;
                    this.OnChange();
                    this.OnPropertyChanged(nameof(this.VmTradeCountText));
                }
            }
        }

        internal CSettings Settings => this.Trader.Settings;
        private bool SettingsDirectoryChanged;
        private DirectoryInfo SettingsDirectoryM;
        internal DirectoryInfo SettingsDirectoryInfo 
        {
            get => CLazyLoad.Get(ref this.SettingsDirectoryM, () => this.Settings.SettingsDirectoryInfo);
            set
            {
                this.SettingsDirectoryM= value;
                this.SettingsDirectoryChanged = true;
                this.OnChange();
                this.OnPropertyChanged(nameof(this.VmSettingsDirectory));
            }
        }
        public DirectoryInfo VmSettingsDirectory => this.SettingsDirectoryInfo;

        #region ProfitMinFaktor
        private readonly decimal ProfitMinFaktorDefault = 0.00001M;
        private bool ProfitMinFaktorChanged;
        private decimal ProfitMinFaktorM;
        internal decimal ProfitMinFaktor
        {
            get => this.ProfitMinFaktorM;
            set
            {
                if(!object.Equals(this.ProfitMinFaktorM, value))
                {
                    this.ProfitMinFaktorChanged = true;
                    this.ProfitMinFaktorM = value;
                    this.OnChange();
                    this.OnPropertyChanged(nameof(this.VmProfitMinFaktorText));
                }
            }
        }
        [XmlElement("ProfitMinFaktor")]
        public string VmProfitMinFaktorText
        {
            get => (this.ProfitMinFaktor * 1).ToString(this.DigitFormat); 
            set => this.ProfitMinFaktor = decimal.Parse(value) / 1;
        }
        #endregion
        #region BuyCurrency
        [XmlIgnore]
        public object VmBuyCurrency => this.BuyCurrency;
        #endregion
        #region ProfitTargetFaktor
        private bool ProfitTargetFaktorChanged;
        private decimal ProfitTargetFaktorM;
        private readonly decimal ProfitTargetFaktorDefault = 0.25M;
        internal decimal ProfitTargetFaktor
        {
            get => this.ProfitTargetFaktorM;
            set
            {
                if (!object.Equals(this.ProfitTargetFaktorM, value))
                {
                    this.ProfitTargetFaktorChanged = true;
                    this.ProfitTargetFaktorM = value;
                    this.OnChange();
                    this.OnPropertyChanged(nameof(this.VmProfitTargetFaktorText));
                }
            }
        }
        [XmlElement("ProfitTargetFaktor")]
        public string VmProfitTargetFaktorText
        {
            get => (this.ProfitTargetFaktor * 100).ToString("0");
            set => this.ProfitTargetFaktor = decimal.Parse(value) / 100;
        }
        #endregion
        #region InvestAmount
        internal CCurrency InvestCurrency => this.Settings.SellCurrency;
        public object VmInvestCurrency => this.InvestCurrency;
        private bool InvestAmountChanged;
        private decimal InvestAmountM;
        internal decimal InvestAmount
        {
            get => this.InvestAmountM;
            set
            {
                if (!object.Equals(this.InvestAmountM, value))
                {
                    this.InvestAmountChanged = true;
                    this.InvestAmountM = value;
                    this.OnChange();
                    this.OnPropertyChanged(nameof(this.VmInvestAmountText));
                }
            }
        }
        [XmlElement("InvestAmount")]
        public string VmInvestAmountText
        {
            get => this.InvestAmount.ToString("0.00");
            set => this.InvestAmount = decimal.Parse(value);
        }
        #endregion
        #region Interpolate
        private CInterpolateSetting InterpolateSettingDefault => new CInterpolateSetting(true, TimeSpan.FromSeconds(60));
        private CInterpolateSetting InterpolateSettingM;
        internal CInterpolateSetting InterpolateSetting
        {
            get => CLazyLoad.Get(ref this.InterpolateSettingM, () => this.InterpolateSettingDefault);
            set
            {
                this.InterpolateSettingM = value;
                this.InterpolateSettingChanged = true;
                this.OnChange();
                this.OnPropertyChanged(nameof(this.VmInterpolateSecondsText));
                this.OnPropertyChanged(nameof(this.VmInterpolateIsEnabled));
            }
        }
        private bool InterpolateSettingChanged;
        [XmlElement("InterpolateSeconds")]
        public string VmInterpolateSecondsText
        {
            get => this.InterpolateSetting.Item2.TotalSeconds.ToString();
            set
            {
                this.InterpolateSetting = new CInterpolateSetting(this.InterpolateSetting.Item1, TimeSpan.FromSeconds(double.Parse(value)));
            }
        }
        [XmlElement("InterpolateIsEnabled")]
        public bool VmInterpolateIsEnabled
        {
            get => this.InterpolateSetting.Item1;
            set => this.InterpolateSetting = new CInterpolateSetting(value, this.InterpolateSetting.Item2);
        }
        #endregion
        #region CoinApiKey
        private string CoinApiKeyM = string.Empty;
        internal string CoinApiKey
        {
            get => this.CoinApiKeyM;
            set
            {
                this.CoinApiKeyM = value;
                this.OnChange();
                this.OnPropertyChanged(nameof(this.VmCoinApiKey));
            }
        }
        [XmlElement("CoinApiKey")]
        public string VmCoinApiKey
        {
            get => this.CoinApiKey;
            set => this.CoinApiKey = value;
        }
        #endregion
        #region Digits
        internal readonly int Digits = 5;
        internal string DigitFormat => "0." + new string('0', this.Digits);
        #endregion
        #region Currency
        internal CCurrencyEnum FromCurrencyEnum => CCurrencyEnum.EUR;
        internal CCurrency SellCurrency => CCurrency.Get(this.FromCurrencyEnum);
        internal CCurrencyEnum ToCurrencyEnum => CCurrencyEnum.XRP;
        internal CCurrency BuyCurrency => CCurrency.Get(this.ToCurrencyEnum);
        #endregion
        internal void TakeEffect()
        {
            if(this.SettingsDirectoryChanged)
            {
                this.Trader.SettingsDirectoryInfo = this.SettingsDirectoryInfo;
            }
            else if(this.InterpolateSettingChanged)
            {
                this.Trader.ExchangeRateHistogram = default;
            }
            else if(this.TradeCountChanged)
            {
                this.Trader.WeightTablesVm = default;
                this.Trader.InvestmentPlan = default;
            }
            else if(this.ProfitMinFaktorChanged
                 || this.ProfitTargetFaktorChanged)
            {
                this.Trader.InvestmentPlan = default;
            }
            else if (this.InvestAmountChanged)
            {
                this.Trader.LimitOrdersVms.RefreshActivateds();
            }            
            if (this.Changed)
            {
                this.Save();
            }
            this.SetChanged(false);
        }
        private void SetChanged(bool aChanged)
        {
            this.SettingsDirectoryChanged = aChanged;
            this.PeriodEnumChanged = aChanged;
            this.TradeCountChanged = aChanged;
            this.ProfitMinFaktorChanged = aChanged;
            this.ProfitTargetFaktorChanged = aChanged;
            this.InvestAmountChanged = aChanged;
            this.InterpolateSettingChanged = aChanged;
            this.Changed = aChanged;
        }
    }
    internal sealed class CSettings
    {
        internal CSettings(CCbTrader aTrader)
        {
            this.Trader = aTrader;
        }

        internal readonly CCbTrader Trader;
        //internal static readonly CSettings Singleton = new CSettings();
        internal static DirectoryInfo RootSettingsDirectoryStatic => new DirectoryInfo(Path.Combine(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)).FullName, "CbTrader"));
        internal DirectoryInfo RootSettingsDirectory => RootSettingsDirectoryStatic;
        internal FileInfo SettingsFileInfo => new FileInfo(Path.Combine(this.SettingsDirectoryInfo.FullName, "Settings.xml"));

        internal CCurrencyEnum FromCurrencyEnum => this.Trader.SettingsVm.FromCurrencyEnum;
        internal CCurrency SellCurrency => this.Trader.SettingsVm.SellCurrency;
        internal CCurrencyEnum ToCurrencyEnum => this.Trader.SettingsVm.ToCurrencyEnum;
        internal CCurrency BuyCurrency => this.Trader.SettingsVm.BuyCurrency;

        internal DirectoryInfo SettingsDirectoryInfoDefault => this.RootSettingsDirectory;

        private FileInfo SettingsDirectoryFileInfo => new FileInfo(Path.Combine(this.RootSettingsDirectory.FullName, "SettingsDirectory.txt"));
        private DirectoryInfo NewSettingsDirectoryInfo()
        {
            var aFileInfo = this.SettingsDirectoryFileInfo;
            aFileInfo.Refresh();
            if (aFileInfo.Exists)
            {
                var aDirectoryFullName = File.ReadAllText(aFileInfo.FullName);
                var aDirectoryInfo = new DirectoryInfo(aDirectoryFullName);
                return aDirectoryInfo;
            }
            else
            {
                var aDirectoryInfo = this.RootSettingsDirectory;
                return aDirectoryInfo;
            }
        }
        //new DirectoryInfo(@"C:\Karle\Cloud\GoogleDrive\Ablage\cryptozock\CbTrader");
        private DirectoryInfo SettingsDirectoryInfoM;
        internal DirectoryInfo SettingsDirectoryInfo
        {
            get => CLazyLoad.Get(ref this.SettingsDirectoryInfoM, this.NewSettingsDirectoryInfo);
            set
            {
                this.SettingsDirectoryInfoM = value;
                File.WriteAllText(this.SettingsDirectoryFileInfo.FullName, value.FullName);
            }
        }
        internal DirectoryInfo CoinApiDirectoryInfo => this.InfoProviderDirectoryInfo(CInfoProviderEnum.CoinApi);
        private DirectoryInfo InfoProviderDirectoryInfo(CInfoProviderEnum aInfoProviderEnum)
            => new DirectoryInfo(Path.Combine(this.SettingsDirectoryInfo.FullName, aInfoProviderEnum.ToString()));
        private DirectoryInfo GetDirectoryInfo(DirectoryInfo aBaseDirectory, CDirectoryEnum aDirectoryEnum)
            => new DirectoryInfo(Path.Combine(aBaseDirectory.FullName, aDirectoryEnum.ToString()));

        internal DirectoryInfo CoinApiExchangesDirectoryInfo => this.GetDirectoryInfo(this.CoinApiDirectoryInfo, CDirectoryEnum.Exchanges);
        internal DirectoryInfo CoinApiExchangeRateDirectoryInfo => this.GetDirectoryInfo(this.CoinApiDirectoryInfo, CDirectoryEnum.ExchangeRates);
        internal DirectoryInfo CoinApiSymbolsDirectoryInfo => this.GetDirectoryInfo(this.CoinApiDirectoryInfo, CDirectoryEnum.Symbols);
        internal DirectoryInfo TrackerDirectoryInfo => this.GetDirectoryInfo(this.SettingsDirectoryInfo, CDirectoryEnum.Tracker);
        internal DirectoryInfo TrackerExchangeRateDirectoryInfo => this.GetDirectoryInfo(this.TrackerDirectoryInfo, CDirectoryEnum.ExchangeRates);

        internal FileInfo NewFileInfo(DirectoryInfo aDirectoryInfo, DateTime aDateTime, string aExtension)
            => new FileInfo(Path.Combine(aDirectoryInfo.FullName, aDateTime.ToString("yyyy-MM-dd-HH-mm-ss") + aExtension));

        private DirectoryInfo GetDirectoryInfo(DirectoryInfo aBaseDirectory, CCurrency aSellCurrency, CCurrency aBuyCurrency)
            => new DirectoryInfo(Path.Combine(aBaseDirectory.FullName, aSellCurrency.Enum.ToString(), aBuyCurrency.Enum.ToString()));

        internal DirectoryInfo GetCoinApiExchangeRateDirectoryInfo(CCurrency aSellCurrency, CCurrency aBuyCurrency)
            => this.GetDirectoryInfo(this.CoinApiExchangeRateDirectoryInfo, aSellCurrency, aBuyCurrency);
        internal DirectoryInfo GetCoinApiHistoricalDataDirectory(CCurrency aSellCurrency, CCurrency aBuyCurrency)
            => this.GetDirectoryInfo(this.InfoProviderDirectoryInfo(CInfoProviderEnum.CoinApi), aSellCurrency, aBuyCurrency);

        internal DirectoryInfo GetTrackerExchangeRateDirectoryInfo(CCurrency aSellCurrency, CCurrency aBuyCurrency)
            => this.GetDirectoryInfo(this.TrackerDirectoryInfo, aSellCurrency, aBuyCurrency);

        internal FileInfo GetExchangeRateTrackerFileInfo()
        {
            var aSellCurrency = this.SellCurrency;
            var aBuyCurrency = this.BuyCurrency;
            var aDirectory = this.GetTrackerExchangeRateDirectoryInfo(aSellCurrency, aBuyCurrency);
            var aFileInfo = new FileInfo(Path.Combine(aDirectory.FullName, "ExchangeRates.txt"));
            return aFileInfo;
        }

        internal FileInfo GetExchangeRatePeriodsFileInfo()
        {
            var aDirectoryInfo = this.GetTrackerExchangeRateDirectoryInfo(this.SellCurrency, this.BuyCurrency);
            var aFileInfo = new FileInfo(Path.Combine(aDirectoryInfo.FullName, "ExchangeRatePeriod.xml"));
            return aFileInfo;
        }
    }
}
