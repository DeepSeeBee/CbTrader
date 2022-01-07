using CbTrader.Domain;
using CbTrader.InfoProviders;
using CbTrader.InfoProviders.CoinApi;
using CbTrader.Settings;
using CbTrader.Tracker;
using CbTrader.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CbTrader
{
    using CGetHistogramPointFunc = Func<Tuple<CExchangeRate, decimal, decimal>>;

    internal static class CLazyLoad
    {
        internal static T Get<T>(ref T aVal, Func<T> aNew) where T : class
        {
            if (!(aVal is object))
            {
                aVal = aNew();
            }
            return aVal;
        }
        internal static T? GetNullable<T>(ref T? aVal, Func<T?> aNew) where T : struct
        {
            if (!aVal.HasValue)
            {
                aVal = aNew();
            }
            return aVal;
        }
        internal static T Get<T>(ref T? aVal, Func<T> aNew) where T : struct
        {
            if (!aVal.HasValue)
            {
                aVal = aNew();
            }
            return aVal.Value;
        }
    }



 
    enum CCurrencyEnum
    {
        EUR,
        XRP,
    }


    internal class CCurrency
    {
        private CCurrency(CCurrencyEnum aCurrencyEnum, string aShortName, string aSymbol) { this.Enum = aCurrencyEnum; this.ShortName = aShortName; this.Symbol = aSymbol; }
        internal static CCurrency Get(CCurrencyEnum aCurrencyEnum)
            => Dic[aCurrencyEnum];      
        internal static CCurrency Get(string aCurrencyEnumName)
            => Get((CCurrencyEnum) System.Enum.Parse(typeof(CCurrencyEnum), aCurrencyEnumName));
        internal static CCurrency FromXml(XmlElement aXmlElement)
        {
            var aCurrencyName = aXmlElement.GetAttribute(CurrencyAttributeName);
            var aCurrency = CCurrency.Get(aCurrencyName);
            return aCurrency;
        }

        internal readonly CCurrencyEnum Enum;
        internal string Name => this.Enum.ToString();
        internal readonly string ShortName;
        internal readonly string Symbol;
        internal static readonly CCurrency Euro = new CCurrency(CCurrencyEnum.EUR, "Euro", "€");
        internal static readonly CCurrency Ripple = new CCurrency(CCurrencyEnum.XRP, "Xrp", default);
        internal string Caption => this.Symbol is object ? this.Symbol : this.ShortName;

        public override string ToString()
            => this.Caption;

        private static Dictionary<CCurrencyEnum, CCurrency> DicM;
        private static Dictionary<CCurrencyEnum, CCurrency> Dic => CLazyLoad.Get(ref DicM, NewDic);
        private static Dictionary <CCurrencyEnum, CCurrency> NewDic()
        {
            var aDic = new Dictionary<CCurrencyEnum, CCurrency>();
            var aAdd = new Action<CCurrency>(delegate (CCurrency aCurrency)
            {
                aDic.Add(aCurrency.Enum, aCurrency);
            });
            aAdd(CCurrency.Euro);
            aAdd(CCurrency.Ripple);
            return aDic;
        }
        private static readonly string CurrencyAttributeName = "Currency";

        internal void SetAttributes(XmlElement aElement)
        {
            aElement.SetAttribute(CurrencyAttributeName, this.Enum.ToString());
        }

        internal static CCurrency Parse(string aCurrencyEnumText)
            => CCurrency.Get((CCurrencyEnum)System.Enum.Parse(typeof(CCurrencyEnum), aCurrencyEnumText));
    }

    internal enum CExchangeDirection
    {
        Buy,
        Sell,
    }

    internal sealed class CPrice
    {
        internal CPrice(decimal aValue) { this.Value = aValue; }

        internal static CPrice FromXml(XmlElement aXmlElement)
        {
            var aValueString = aXmlElement.GetAttribute(ValueAttributeName);
            var aValueDecimal = decimal.Parse(aValueString);
            var aAmount = new CPrice(aValueDecimal);
            return aAmount;
        }
        internal readonly decimal Value;
        private static readonly string ValueAttributeName = "Value";

        public static CPrice operator *(CPrice lhs, CPrice rhs)
            => lhs * rhs.Value;
        public static CPrice operator *(CPrice lhs, decimal rhs)
            => new CPrice(lhs.Value * rhs);
        public override string ToString()
            => this.Value.ToString("0.00000");

        internal string ToXmlString()
        {
            var aXmlDoc = new XmlDocument();
            var aElement = aXmlDoc.CreateElement("Amount");
            this.SetAttriutes(aElement);
            var aOuterXml = aElement.OuterXml;
            return aOuterXml;
        }
        internal void SetAttriutes(XmlElement aElement)
        {
            aElement.SetAttribute(ValueAttributeName, this.Value.ToString());
        }

        internal CPrice Invert()
            => new CPrice(1M / this.Value);

        internal CPrice Invert(bool aInvert)
            => aInvert ? this.Invert() : this;

    }

    internal sealed class CPriceWithCurrency
    {
        internal CPriceWithCurrency(CPrice aAmount, CCurrency aCurrency)
        {
            this.Amount = aAmount;
            this.Currency = aCurrency;
        }
        internal static CPriceWithCurrency FromXml(XmlElement aXmlElement)
        {
            var aAmount = CPrice.FromXml(aXmlElement);
            var aCurrency = CCurrency.FromXml(aXmlElement);
            var aAmountWithCurrency = new CPriceWithCurrency(aAmount, aCurrency);
            return aAmountWithCurrency;
        }

        internal readonly CPrice Amount;
        internal readonly CCurrency Currency;
        public override string ToString()
            => this.Amount.ToString() + " " + this.Currency.ToString();
        public static CPriceWithCurrency operator *(CPriceWithCurrency lhs, CPriceWithCurrency rhs)
            => lhs.Currency == rhs.Currency ? new CPriceWithCurrency(lhs.Amount * rhs.Amount, lhs.Currency) : throw new InvalidOperationException();
        public static CPriceWithCurrency operator *(CPriceWithCurrency lhs, decimal rhs)
            => new CPriceWithCurrency(lhs.Amount * rhs, lhs.Currency);

        internal CPriceWithCurrency Exchange(CPriceWithCurrency aRate)
            => this.Exchange(aRate, aRate.Currency);

        internal CPriceWithCurrency Exchange(CPriceWithCurrency aRate, CCurrency aCurrency)
        {
            if (aRate.Currency == this.Currency)
                return new CPriceWithCurrency(new CPrice(this.Amount.Value / aRate.Amount.Value), aCurrency);
            else if (aRate.Currency == aCurrency)
                return new CPriceWithCurrency(new CPrice(this.Amount.Value * aRate.Amount.Value), aCurrency); 
            else
                throw new ArgumentException();
        }

        internal void SetAttributes(XmlElement aEl)
        {
            this.Amount.SetAttriutes(aEl);
            this.Currency.SetAttributes(aEl);
        }

        internal CPriceWithCurrency Invert(CCurrency aCurrency)
            =>new CPriceWithCurrency(this.Amount.Invert(), aCurrency);

        internal CPriceWithCurrency Invert()
            => this.Invert(this.Currency);
    }

    internal class CExchange
    {
        internal CExchange(CPriceWithCurrency aSell, CPriceWithCurrency aBuy)
        {
            this.Sell = aSell;
            this.Buy = aBuy;
        }
        internal readonly CPriceWithCurrency Sell;
        internal readonly CPriceWithCurrency Buy;
        internal static readonly string XmlElementName = "Exchange";

        internal string ToXml()
        {
            var aDoc = new XmlDocument();
            var aElement = aDoc.CreateElement(XmlElementName);
            aElement.SetAttribute("SellCurrency", this.Sell.Currency.Symbol);
            aElement.SetAttribute("SellAmount", this.Sell.Amount.Value.ToString());
            aElement.SetAttribute("BuyCurrency", this.Buy.Currency.Symbol);
            aElement.SetAttribute("BuyAmount", this.Buy.Amount.Value.ToString());
            var aXml = aElement.OuterXml;
            return aXml;
        }
    }
    internal class CPriceWithDateTime
    {
        internal CPriceWithDateTime(DateTime aDateTime, CPrice aAmount)
        {
            this.DateTime = aDateTime;
            this.Amount = aAmount;
        }
        internal DateTime DateTime;
        internal CPrice Amount;
    }

    internal sealed class CExchangeRate
    {
        internal CExchangeRate(DateTime aDateTime, CCurrency aSellCurrency, CPriceWithCurrency aBuyAmount)
        {
            this.DateTime = aDateTime;
            this.SellCurrency = aSellCurrency;
            this.BuyAmount = aBuyAmount;
        }
        internal static CExchangeRate FromXml(XmlElement aXmlElement)
        {
            var aDateTimeString = aXmlElement.GetAttribute(DateTimeAttributeName);
            var aDateTime = DateTime.ParseExact(aDateTimeString, DateTimeFormat, null);
            var aSellCurrencyText = aXmlElement.GetAttribute(SellCurrencyAttributeName);
            var aSellCurrency = CCurrency.Parse(aSellCurrencyText);
            var aAmountWithCurrency = CPriceWithCurrency.FromXml(aXmlElement);
            var aExchangeRate = new CExchangeRate(aDateTime, aSellCurrency, aAmountWithCurrency);
            return aExchangeRate;
        }
        internal readonly DateTime DateTime;
        internal readonly CCurrency SellCurrency;
        private static readonly string SellCurrencyAttributeName = "SellCurrency";
        private static readonly string DateTimeAttributeName = "DateTime";
        internal readonly CPriceWithCurrency BuyAmount;
        internal static readonly string XmlElementName = "ExchangeRate";
        private static readonly string DateTimeFormat = "R";
        internal static bool BuyCurrencyIsEqual(CCurrency aBuyCurrency, params CExchangeRate[] aRates)
        {
            foreach (var aRate in aRates)
            {
                if (aRate.BuyAmount.Currency.Enum != aBuyCurrency.Enum)
                    return false;
            }
            return true;
        }
        internal static bool SellCurrencyIsEqual(CCurrency aSellCurrency, params CExchangeRate[] aRates)
        {
            foreach (var aRate in aRates)
            {
                if (aRate.SellCurrency.Enum != aSellCurrency.Enum)
                    return false;
            }
            return true;
        }
        internal static bool CurrenciesAreEqual(CCurrency aSellCurrency, CCurrency aBuyCurrency, params CExchangeRate[] aRates)
        {
            var aEqual = SellCurrencyIsEqual(aSellCurrency, aRates)
                      && BuyCurrencyIsEqual(aBuyCurrency, aRates)
                      ;
            return aEqual;
        }
        internal void ThrowIfNotCurrenciesAreEqual(params CExchangeRate[] aRates)
        {
            if (!CurrenciesAreEqual(this.SellCurrency, this.BuyAmount.Currency, aRates))
            {
                throw new ArgumentException(this.GetType().Name + ": Currencies missmatch.");
            }
        }
        internal static CExchangeRate Interpolate(CExchangeRate aLeft, CExchangeRate aRight)
        {
            aLeft.ThrowIfNotCurrenciesAreEqual(aRight);
            var aTime1 = aLeft.DateTime;
            var aTime2 = aRight.DateTime;
            var aMinTime = aTime1.Min(aTime2);
            var aMaxTime = aTime1.Max(aTime2);
            var aTime3 = aMinTime.Add(aMaxTime.Subtract(aMinTime));
            var aValue1 = aLeft.BuyAmount.Amount.Value;
            var aValue2 = aRight.BuyAmount.Amount.Value;
            var aMinValue = Math.Min(aValue1, aValue2);
            var aMaxValue = Math.Max(aValue1, aValue2);
            var aDiff = aMaxValue - aMinValue;
            var aValue3 = aMinValue + aDiff / 2M;
            var aInterpolatedTime = aTime3;
            var aInterpolatedValue = aValue3;
            var aInterpolatedBuyPrice = new CPriceWithCurrency(new CPrice(aInterpolatedValue), aLeft.BuyAmount.Currency);
            var aInterpolatedRate = new CExchangeRate(aInterpolatedTime, aLeft.SellCurrency, aInterpolatedBuyPrice);
            return aInterpolatedRate;
        }
        public override string ToString()
            => this.TitleWithoutDate;

        internal string TitleWithoutDate
            => this.BuyAmount.Invert(this.SellCurrency).ToString() + "/" + this.BuyAmount.Currency.ToString();
        //=>this.BuyAmount.Invert().ToString() + "/" + this.SellCurrency.ToString();

        internal string ToXmlString()
        {
            var aDoc = new XmlDocument();
            var aEl = aDoc.CreateElement(XmlElementName);
            aEl.SetAttribute(DateTimeAttributeName, this.DateTime.ToString(DateTimeFormat));
            aEl.SetAttribute(SellCurrencyAttributeName, this.SellCurrency.Enum.ToString());
            this.BuyAmount.SetAttributes(aEl);
            var aXmlString = aEl.OuterXml;
            return aXmlString;
        }
    }

    internal sealed class CExchangeRateHistogram
    {
        internal CExchangeRateHistogram(CCbTrader aTrader, CCurrency aSellCurrency, CCurrency aBuyCurrency, CExchangeRate[] aExchangeRates)
        {
            this.Trader = aTrader;
            this.SellCurrency = aSellCurrency;
            this.BuyCurrency = aBuyCurrency;            
            this.Items = aExchangeRates;
        }

        private CExchangeRate TrendLineStartM;
        internal CExchangeRate TrendLineStart => CLazyLoad.Get(ref this.TrendLineStartM, () => this.NewTrendLine(this.Items.First().DateTime, this.MiddleDateTime));
        private CExchangeRate TrendLineEndM;
        internal CExchangeRate TrendLineEnd => CLazyLoad.Get(ref this.TrendLineEndM, () => this.NewTrendLine(this.MiddleDateTime, this.Items.Last().DateTime));

        private CExchangeRate NewTrendLine(DateTime aStart, DateTime aEnd)
        => new CExchangeRate(this.GetMiddle(aStart, aEnd), this.SellCurrency, new CPriceWithCurrency(new CPrice(this.GetAverage(this.Within(aStart, aEnd))), this.BuyCurrency));
        
        private decimal GetAverage(IEnumerable<CExchangeRate> aRates)
            => aRates.Select(r => r.BuyAmount.Amount.Value).Average();
        private decimal? TrendLinePitchM;
        internal decimal TrendLinePitchReal => CLazyLoad.Get(ref this.TrendLinePitchM, this.NewNeigung);
        internal decimal TrendLinePitchCoerced => Math.Min(1, Math.Max(-1, this.TrendLinePitchReal));
        internal static string TrendLinePitchTitle(decimal aTrendLinePitch, bool aIncludePropertyName = true)
            => (aIncludePropertyName ? "Trend-Line Pitch: " : string.Empty) + (aTrendLinePitch * 100).ToString("0.0") + "%";
        public string VmTrendLinePitchTitle => TrendLinePitchTitle(this.TrendLinePitchReal);
        private decimal  NewNeigung()
        {
            var aFactor = 2;
            var s = this.TrendLineStart.BuyAmount.Amount.Invert().Value;
            var e = this.TrendLineEnd.BuyAmount.Amount.Invert().Value;
            var min = this.MaxY.BuyAmount.Amount.Invert().Value;
            var max = this.MinY.BuyAmount.Amount.Invert().Value;
            var se_min = Math.Min(s, e);
            var se_max = Math.Max(s, e);
            var se_d = se_max - se_min;
            var d = max - min;
            var r = d == 0 ? 0 : se_d / d;
            var n = r * Math.Sign(e-s) * aFactor;
            var nn = Math.Max(-1, Math.Min(1, n));
            return n;
        }
        private DateTime GetMiddle(DateTime aMin, DateTime aMax)
        {
            var aDuration = aMax.Subtract(aMin);
            var aMiddleTimeSpan = TimeSpan.FromHours(aDuration.TotalHours * 0.5);
            var aMid = aMin.Add(aMiddleTimeSpan);
            return aMid;
        }
        internal DateTime MiddleDateTime => this.GetMiddle(this.Items.First().DateTime, this.Items.Last().DateTime);

        internal readonly CCbTrader Trader;
        internal static CExchangeRateHistogram Load(CCbTrader aTrader)
        {
            var aSettings = aTrader.Settings;
            var aBuyCurrency = aSettings.BuyCurrency;
            var aSellCurrency = aSettings.SellCurrency;
            var aFileInfo = aSettings.GetExchangeRateTrackerFileInfo();
            aFileInfo.Refresh();
            if (aFileInfo.Exists)
            {
                var aElementName = CExchangeRate.XmlElementName;
                var aLines1 = new string[] { "<" + aElementName + ">" };
                var aLines2 = File.ReadAllLines(aFileInfo.FullName);
                var aLines3 = new string[] { "</" + aElementName + ">" };
                var aLines = aLines1.Concat(aLines2).Concat(aLines3);
                var aXml = string.Join(string.Empty, aLines);
                var aXmlDoc = new XmlDocument();
                aXmlDoc.LoadXml(aXml);
                var aXmlElements = aXmlDoc.DocumentElement.SelectNodes(aElementName);
                var aExchangeRates = aXmlElements.Cast<XmlElement>().Select(aEl => CExchangeRate.FromXml(aEl)).ToArray();

                var aHistogram = new CExchangeRateHistogram(aTrader, aSellCurrency, aBuyCurrency, aExchangeRates);
                return aHistogram;
            }
            else
            {
                var aHistogram = new CExchangeRateHistogram(aTrader, aSellCurrency, aBuyCurrency, new CExchangeRate[] { });
                return aHistogram;
            }
        }

        //internal CExchangeRateHistogram Normalize(TimeSpan aIntervall)
        //{
        //    var aMinDateTime = this.MinDateTimeAmount.DateTime;
        //    var aMaxDateTime = this.MaxDateTimeAmount.DateTime;


        //}

        //internal CAmountHistogram()
        //:
        //    this(
        //            CCurrency.Euro,
        //            CCurrency.Ripple,
        //            new CAmountWithDateTime[]{
        //            new CAmountWithDateTime(new DateTime(2021,1,1), new CAmount((decimal)1)),
        //            new CAmountWithDateTime(new DateTime(2021,1,2), new CAmount((decimal)1.00001))
        //            }
        //        )
        //{
        //}

        internal readonly CCurrency SellCurrency;
        internal readonly CCurrency BuyCurrency;
        internal readonly CExchangeRate[] Items;

        internal bool ContainsOneOrMoreValues => this.Items.Length > 0;

        private int? MaxYIndexM;
        internal int MaxYIndex => CLazyLoad.Get(ref this.MaxYIndexM, () => this.Items.FindMaxIndex(v => v.BuyAmount.Amount.Value).Value);
        internal CExchangeRate MaxY => this.Items[this.MaxYIndex];

        private int? MinYIndexM;
        internal int MinYIndex => CLazyLoad.Get(ref this.MinYIndexM, () => this.Items.FindMinIndex(v => v.BuyAmount.Amount.Value).Value);
        internal CExchangeRate MinY => this.Items[this.MinYIndex];
        internal CExchangeRate Newest => this.Items.Last();
        internal CExchangeRate Oldest => this.Items.First();

        public Tuple<DateTime, DateTime> DateRange => new Tuple<DateTime, DateTime>(this.Items.First().DateTime, this.Items.Last().DateTime);

        #region View
        private TimeSpan GetXTimeSpan(DateTime aDateTime)
            => aDateTime.Subtract(this.Oldest.DateTime);
        private TimeSpan GetXTimeSpan(CExchangeRate aExchangeRate)
            => this.GetXTimeSpan(aExchangeRate.DateTime);
        private decimal GetXValue(CExchangeRate aExchangeRate)
            => (decimal)this.GetXTimeSpan(aExchangeRate).TotalHours;
        private decimal GetXFkt(CExchangeRate aExchangeRate)
            => this.GetXValue(aExchangeRate) / this.GetXValue(this.Newest);

        private decimal GetYFkt(CExchangeRate aExchangeRate)
            => (aExchangeRate.BuyAmount.Amount.Value - this.MinY.BuyAmount.Amount.Value) / (this.MaxY.BuyAmount.Amount.Value-this.MinY.BuyAmount.Amount.Value);

        internal IEnumerable<CGetHistogramPointFunc>GetPointFuncs(Func<decimal> aGetWidth, Func<decimal> aGetHeight)
            => this.GetPointFuncs(aGetWidth, aGetHeight, this.Items);
        internal IEnumerable<CGetHistogramPointFunc> GetPointFuncs(Func<decimal> aGetWidth, Func<decimal> aGetHeight, params CExchangeRate[] aDataPoints)
            => aDataPoints.Select(er => new Func<Tuple<CExchangeRate, decimal, decimal>>(() => new Tuple<CExchangeRate, decimal, decimal>(er, aGetWidth() * GetXFkt(er), aGetHeight() * GetYFkt(er))));
        internal IEnumerable<CExchangeRate> Within(DateTime aStartDate, DateTime aEndDate)
        {
            var aPoints1 = this.Items;
            var aPoints2 = aPoints1.Where(p => p.DateTime.CompareTo(aStartDate) >= 0 && p.DateTime.CompareTo(aEndDate) <= 0);
            return aPoints2;
        }

        internal CExchangeRateHistogram NewWithin(DateTime aStartDate, DateTime aEndDate)
        {
            var aPoints = this.Within(aStartDate, aEndDate).ToArray();
            var aHistogram = new CExchangeRateHistogram(this.Trader, this.SellCurrency, this.BuyCurrency, aPoints);
            return aHistogram;
        }
        #endregion

        internal CExchangeRateHistogram Interpolate(TimeSpan aIntervall)
        {
            var aSellCurrency = this.SellCurrency;
            var aBuyCurrency = this.BuyCurrency;
            var aItems = this.Items;
            var aFirstTime = aItems.First().DateTime;
            var aLastTime = aItems.Last().DateTime;
            var aDeltaTime = aLastTime - aFirstTime;
            var aInterpolatedCount = (int)(aDeltaTime.TotalMinutes / aIntervall.TotalMinutes) + 1;
            var aInterpolatedPoints = new List<CExchangeRate>(aInterpolatedCount);
            var aPoint1 = aItems.First();
            var aIdx = 1;
            aInterpolatedPoints.Add(aPoint1);
            if (aItems.Length > 1)
            {
                do
                {
                    var aTime = aPoint1.DateTime;
                    var aNextTime = aFirstTime.Add(TimeSpan.FromMinutes(aIntervall.TotalMinutes * aInterpolatedPoints.Count));
                    while (aIdx < aItems.Length - 1
                        && aItems[aIdx].DateTime.Subtract(aTime).CompareTo(aIntervall) < 0)
                        ++aIdx;
                    var aPoint2 = aItems[aIdx];
                    var aP1 = aPoint1.BuyAmount.Amount.Value;
                    var aP3 = aPoint2.BuyAmount.Amount.Value;
                    var aPD = aP3 - aP1;
                    var aT1 = aPoint1.DateTime;
                    var aT2 = aNextTime;
                    var aT3 = aPoint2.DateTime;
                    var aDMax = aT3.Subtract(aT1);
                    var aDPart = aT2.Subtract(aT1);
                    var aFkt = (decimal)(aDPart.TotalSeconds / aDMax.TotalSeconds);
                    var aP2 = aP1 + aPD * aFkt;
                    var aInterpolatedPrice = aP2;
                    var aInterpolatedTime = aNextTime;
                    var aInterpolatedPoint = new CExchangeRate(aInterpolatedTime, aSellCurrency, new CPriceWithCurrency(new CPrice(aInterpolatedPrice), aBuyCurrency));
                    aInterpolatedPoints.Add(aInterpolatedPoint);
                    aPoint1 = aInterpolatedPoint;
                }
                while (aIdx < this.Items.Length - 1);
            }
            var aInterpolatedHistogram = new CExchangeRateHistogram(this.Trader, aSellCurrency, aBuyCurrency, aInterpolatedPoints.ToArray());
            return aInterpolatedHistogram;
        }

    }



    internal sealed class CInvestmentPlan
    {
        internal CInvestmentPlan(CExchangeRateHistogram aAmountHistogram, CSettingsVm aSettingsVm)
        {
            this.AmountHistogram = aAmountHistogram;
            this.InvestAmount = new CPriceWithCurrency(new CPrice(aSettingsVm.InvestAmount), aSettingsVm.InvestCurrency);
            this.BuyCount = aSettingsVm.TradeCount;
            this.ProfitMinFkt = aSettingsVm.ProfitMinFaktor;
            this.ProfitTargetFkt = aSettingsVm.ProfitTargetFaktor;            
            this.LookbackTimespan = aSettingsVm.LookBackTimeSpan;
            this.Digits = aSettingsVm.Digits;
        }
        //internal CInvestmentPlan() 
        //: 
        //    this
        //    (
        //        new CAmountHistogram(),
        //        new CAmountWithCurrency(new CAmount((decimal)333), CCurrency.Euro)
        //    )
        //{
        //}
        internal readonly CExchangeRateHistogram AmountHistogram;
        internal CPriceWithCurrency InvestAmount;

        internal int BuyCount = 3;
        internal decimal BuyRangeMaxFkt = 1;
        internal decimal ProfitTargetFkt;
       // internal readonly decimal DiffMinFkt = 0.00001M;
        internal readonly decimal ProfitMinFkt = 0.001M;
        internal readonly CPeriodEnum PeriodEnum = CPeriodEnum.Minutes1;
        internal readonly TimeSpan LookbackTimespan =  TimeSpan.FromMinutes(180);
        internal readonly int Digits;
        internal decimal Round(decimal aValue)
        {
            var aDigits1 = this.Digits;
            var aPow = (decimal) Math.Pow(10, aDigits1);
            var aValue1 = aValue * aPow;
            var aValue2 = Math.Floor(aValue1);
            var aValue3 =  aValue2 / aPow;
            var aValue4 = (decimal)aValue3;
            return aValue4;
        }
        internal CBuyLimitOrder[] NewBuyLimitOrders()
        {
            if (this.AmountHistogram.ContainsOneOrMoreValues)
            {
                var aInvestmentPlan = this;
                var aBuyCount = aInvestmentPlan.BuyCount;
                var aItems = Enumerable.Range(0, aBuyCount).Select(i =>
                {
                    var aAmountHistogram = aInvestmentPlan.AmountHistogram;
                    var aTotalBuyAmount = aInvestmentPlan.InvestAmount;
                    var aBuyCurrency = aAmountHistogram.BuyCurrency;
                    var aSellCurrency = aAmountHistogram.SellCurrency;
                    var aMax = 1M / aAmountHistogram.MinY.BuyAmount.Amount.Value;
                    var aMin = 1M / aAmountHistogram.MaxY.BuyAmount.Amount.Value;
                    var aDiff = aMax - aMin;
                    var aMid = aMin + aDiff / 2;
                    var aBuyRangeFkt = aInvestmentPlan.BuyRangeMaxFkt;
                    var aItemBuyAmountFkt = (decimal)1 / aBuyCount;
                    var aBuyRateFkt = aBuyCount == 1
                                    ? (decimal)1
                                    : ((decimal)i) / (decimal)(aBuyCount - 1) * aBuyRangeFkt
                                    ;
                    var aBuyRate = (aMid + aDiff /2M ) - (aDiff  * aBuyRateFkt);
                    var aItemBuyAmount = new CPriceWithCurrency(new CPrice(this.Round(aTotalBuyAmount.Amount.Value * aItemBuyAmountFkt)), aTotalBuyAmount.Currency);
                    var aMaxProfit = aMax - aBuyRate; // € / XRP
                    var aWantProfit = Math.Max(aMaxProfit * aInvestmentPlan.ProfitTargetFkt, aInvestmentPlan.ProfitMinFkt);// €/XRP
                    var aSellRate1 = new CPriceWithCurrency(new CPrice(this.Round(aBuyRate + aWantProfit)), aSellCurrency);
                    var aBuyLimitOrder = new CBuyLimitOrder(new CPriceWithCurrency(new CPrice(this.Round(1M/ aBuyRate)), aBuyCurrency), aItemBuyAmount, aSellRate1);
                    return aBuyLimitOrder; 
                    //var aAmountHistogram = aInvestmentPlan.AmountHistogram;
                    //var aTotalBuyAmount = aInvestmentPlan.InvestAmount;
                    //var aBuyCurrency = aAmountHistogram.BuyCurrency;
                    //var aSellCurrency = aAmountHistogram.SellCurrency;
                    //var aMin = aAmountHistogram.MinY.BuyAmount.Amount.Value;
                    //var aMax = aAmountHistogram.MaxY.BuyAmount.Amount.Value;
                    //var aDiff = aMax - aMin;
                    //var aMid = aMin + aDiff / 2;
                    //var aBuyRangeFkt = aInvestmentPlan.BuyRangeMaxFkt;
                    //var aItemBuyAmountFkt = (decimal)1 / aBuyCount;
                    //var aBuyRateFkt = aBuyCount == 1
                    //                ? (decimal)1
                    //                : ((decimal)i) / (decimal)(aBuyCount - 1) * aBuyRangeFkt
                    //                ;
                    //var aBuyRate = new CPriceWithCurrency(new CPrice(aMid - (aDiff / 2 * aBuyRateFkt)), aBuyCurrency);
                    //var aItemBuyAmount = aTotalBuyAmount * aItemBuyAmountFkt;
                    //var aMaxProfit = 1M / aBuyRate.Amount.Value - 1M / aMax; // XRP/€
                    //var aWantProfit = Math.Max(aMaxProfit * aInvestmentPlan.SellGainRange, aMaxProfit * aInvestmentPlan.ProfitMinFkt);// XRP/€
                    //var aSellRate = new CPriceWithCurrency(new CPrice(1M / aBuyRate.Amount.Value + aWantProfit), aSellCurrency);
                    //var aBuyLimitOrder = new CBuyLimitOrder(aBuyRate, aItemBuyAmount, aSellRate);
                    //return aBuyLimitOrder;
                }).ToArray();
                return aItems;
            }
            else
            {
                return new CBuyLimitOrder[] { };
            }
        }
    }

    internal sealed class CBuyLimitOrderVm :CViewModel
    {
        internal CBuyLimitOrderVm( CBuyLimitOrderVms aBuyLimitOrderVms, CBuyLimitOrder aBuyLimitOrder, int aIndex, int aCount)
        {
            this.BuyLimitOrderVms = aBuyLimitOrderVms;
            this.Original = aBuyLimitOrder;
            this.Count = aCount;
            this.Index = aIndex;
        }
        internal readonly int Index;
        internal readonly int Count;
        internal double MidIndexFkt
            => this.Index <= this.Count / 2
            ? ((double)this.Index)  * 2.0d / (this.Count -1)
            : 1 - ((double)this.Index / (this.Count - 1) - 0.5) * 2d
            ;
        private readonly double WeightMin = 0.0001d;
        private double WeightDefault() => Math.Max(WeightMin, this.MidIndexFkt);
        private double WeightDefault(int i) => this.BuyLimitOrderVms.Items[i].WeightDefault();

        private double? WeightM;
        internal double? WeightNullable
        {
            get => this.WeightM;
            set
            {
                this.WeightM = value.HasValue ? Math.Max(WeightMin, value.Value): value;
                this.BuyLimitOrderVms.RefreshActivateds();
                this.OnPropertyChanged(nameof(this.VmWeight));
            }
        }
        internal double WeightInternal
        {
            get => this.WeightM.HasValue ? this.WeightM.Value : 0; // this.WeightWithPitch;
            set
            {
                this.WeightNullable = value;
            }
        }
        internal double Weight
        {
            //get => CLazyLoad.Get(ref this.WeightM, () => this.WeightDefault);
            get => this.WeightInternal; // this.WeightM.HasValue ? this.WeightM.Value : 0; // this.WeightWithPitch;
            set
            {
                //var aDont = value == 0 && this.Active;
                //if(aDont)
                //{
                //    System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new Action(delegate ()
                //    {
                //        this.OnPropertyChanged(nameof(this.VmWeight));
                //    }));
                //}
                //else
                //{
                    this.WeightInternal = value;
                    this.BuyLimitOrderVms.RefreshWeights();
                   // this.OnActiveIsEnabledChanged();
                //}
            }
        }
        internal void RefreshWeight()
        {
            this.OnPropertyChanged(nameof(this.VmWeight));
        }
        internal CCbTrader Trader =>this.BuyLimitOrderVms.Trader;
        internal double TrendLinePitch =>(double) this.Trader.TrendLinePitch;
        private double wwp_positive_p_or_0()
        {
            var p1 = this.TrendLinePitch;
            var p2 = p1 > 0 ? p1 : 0;
            return p2;
        }
        private double wwp1(double i, double w)
        {
            var pp = wwp_positive_p_or_0();// this.TrendLinePitch;
            var c = (double)this.Count;
            var i_fkt = i / c;
            var f = 1 - i_fkt;
            var r = 1 - w;
            var w1 = w + r * pp;
            var f2 = i_fkt * 2;
            var w2 = w1 - 0.5 * f2 * pp;
            return w2;
        }

        private double wwp2(int ii, double w)
        {
            var i = (double)ii;
            var pp = wwp_positive_p_or_0();
            var c = (double)this.Count;
            var f1 = i / c;
            var i2 = c - i - 1;

            var f2 = i / (c - 1);
            var w1 = (f1 < 0.5)
            ? wwp1(i, this.WeightDefault(ii))
            : wwp1(i2, this.WeightDefault(ii));
            var w2 = f1 < 0.5
                ? w1
                : w1 - pp * (f2 - 0.5) * 2;

            if (pp > 0)
                return w2;

            if (f1 >= 0.5)
                return w2;
                //    throw new ArgumentException();

            var np = this.TrendLinePitch;
            var f3 = 1 - ((f1 - 0.5) * 2);
            var w3 = w2 - f1 * -np;
            return w3;
        }
        internal double WeightWithPitch
        {
            get
            {
                //var w = this.WeightDefault;
                //var pp = wwp_positive_p_or_0();
                //var c = (double)this.Count;
                //var i = (double)this.Index;               
                //var f1 = i / c;
                //var f2 = i / (c - 1);
                //var w1 = (f1 < 0.5)
                //? wwp1(i)
                //: wwp1(c - i - 1);
                //var w2 = f1 < 0.5
                //    ? w1 
                //    : w1 - pp * (f2 - 0.5) * 2;

                //if (pp > 0)
                //    return w2;
                //var np = this.TrendLinePitch;
                //var f3 = 1 - ((f1 - 0.5) * 2);
                //var w3 = f1 < 0.5
                //    ? w2 - f1 * -np // ok
                //    : w2 + f3 * 0.25 * np
                //    ;
                //return w3;
                // ab hier
                var w = this.WeightDefault();
                var pp = wwp_positive_p_or_0();
                var ii = this.Index;
                var i = (double)ii;
                var c = (double)this.Count;
                var f1 = i / c;
                var w3 = f1 < 0.5
                       ? this.wwp2(ii, w)
                       : pp > 0
                       ? this.wwp2(ii, w)
                       : this.wwp3() //((this.wwp2(i) + this.wwp2(c-i-1)) / 2)
                       ;
                return w3;
            }
        }

        private int wwp_index_repeat()
            => this.wwp_index_repeat(this.Index, this.Count);
        private int wwp_index_repeat(int i, int c)
        {
            return i - c / 2;
        }
        private int wwp_index_repeat_inverse()
            => wwp_index_repeat_inverse(this.Index, this.Count);

        private int wwp_index_repeat_inverse(int i, int c)
        {
            var i2 = c - i-1;
            return i2;
        }
        private double wwp3()
        {
            //var pp = wwp_positive_p_or_0();
            var p = this.TrendLinePitch;
            var i = (double)this.Index;
            var iri = wwp_index_repeat_inverse();
            var ir = wwp_index_repeat();
            var c = (double)this.Count;
            var wpa1 = this.wwp2(iri, this.WeightDefault(iri));
            var wpa2 = wpa1 * (1 +p);

            var wpb1 = 1- this.wwp2(ir, this.WeightDefault(iri));
            var wpb2 = wpb1 * (1 + p);

            var wp = (wpa2 + wpb2) / 2;
            return wpb2;
        }

        internal double WeightAverage => this.BuyLimitOrderVms.ActiveItems.Select(i => i.Weight).Sum() / (double)this.BuyLimitOrderVms.ActiveItems.Count();
        internal decimal WeightRelative => this.Active ?  (decimal)( this.Weight / this.WeightAverage) : 0M;
        internal bool ActiveIsEnabled
            => this.Weight > 0;
        public bool VmActiveIsEnabled
            => this.ActiveIsEnabled;
        internal void OnActiveIsEnabledChanged()
            => this.OnPropertyChanged(nameof(this.VmActiveIsEnabled));
        public double VmWeight
        {
            get => this.Weight;
            set => this.WeightInternal = value;
        }
        internal readonly CBuyLimitOrderVms BuyLimitOrderVms;
        internal readonly CBuyLimitOrder Original;
        public override string ToString()
            => this.Original.ToString();

        public string Text
        {
            get
            {
                var aActive = this.Active;
                if(aActive)
                {
                    var aActivated = this.Activated;
                    return aActivated.ToString();
                }
                else
                {
                    var aOriginal = this.Original;
                    return aOriginal.TitleWithRateOnly;
                }
            }
        }
        public string VmTextVm => this.Text;

        #region Active
        private void OnTextChanged()
            => this.OnPropertyChanged(nameof(this.VmTextVm));
        private bool ActiveM;
        internal bool Active
        {
            get => this.ActiveM;
            set
            {
                this.ActiveM = value;
                //this.OnPropertyChanged(nameof(this.Active));
                this.OnTextChanged();
                this.BuyLimitOrderVms.RefreshActivateds();
                this.OnPropertyChanged(nameof(this.VmActive));
            }
        }
        public bool VmActive
        {
            get => this.Active;
            set => this.Active = value;
        }
        #endregion

        private CBuyLimitOrder ActivatedM;
        internal CBuyLimitOrder Activated
        {
            get => CLazyLoad.Get(ref this.ActivatedM, this.NewActivated);
            set 
            {
                this.ActivatedM = value;
                this.OnPropertyChanged(nameof(this.ActivatedM));
                this.OnActiveIsEnabledChanged();
                this.OnTextChanged();
            }
        }
        private CBuyLimitOrder NewActivated()
        {
            var aOriginal = this.Original;
            var aInvestAmount = this.BuyLimitOrderVms.Trader.SettingsVm.InvestAmount;
            var aCount = this.BuyLimitOrderVms.Items.Where(i => i.Active).Count();
            var aSellAmount = new CPriceWithCurrency(new CPrice(aInvestAmount / aCount * this.WeightRelative), this.Original.SellAmount.Currency);
            var aActivated = new CBuyLimitOrder(this.Original.BuyRate, aSellAmount, aOriginal.SellLimitOrder.Rate);
            return aActivated;
        }
    }
    internal sealed class CBuyLimitOrderVms : CViewModel
    {
        internal CBuyLimitOrderVms(CCbTrader aTrader, CBuyLimitOrder[] aBuyLimitOrders)
        {
            this.Trader = aTrader;
            var aCount = aBuyLimitOrders.Length;
            this.Count = aCount;
            this.Items = Enumerable.Range(0, this.Count)
                .Select(i => new CBuyLimitOrderVm(this, aBuyLimitOrders[i], i, aCount)).ToArray();
        }
        internal readonly CCbTrader Trader;
        internal readonly int Count;
        internal readonly CBuyLimitOrderVm[] Items;
        public IEnumerable<CBuyLimitOrderVm> VmItems => this.Items;

        internal IEnumerable<CBuyLimitOrderVm> ActiveItems => this.Items.Where(i => i.Active);

        internal void RefreshActivateds()
        {
            foreach (var aItem in this.ActiveItems)
            {
                aItem.Activated = default;
            }
        }
        internal void RefreshWeights()
        {
            var aWeightTablesVm = this.Trader.WeightTablesVm;
            var aPitch = this.Trader.TrendLinePitch;
            var aWeights = aWeightTablesVm.GetWeights((double)aPitch);
            var aItemAndWeights = Enumerable.Range(0, aWeights.Length)
                .Select(i => new Tuple<CBuyLimitOrderVm, double>(this.Items.ElementAt(i), aWeights[i]));
            foreach(var aItemAndWeight in aItemAndWeights)
            {
                aItemAndWeight.Item1.WeightInternal = aItemAndWeight.Item2;
            }
        }
    }


    internal abstract class CLimitOrder
    {
        internal CLimitOrder(CPriceWithCurrency aExchangeRate, CPriceWithCurrency Amount)
        {
            this.Rate = aExchangeRate;
            this.Amount = Amount;
        }
        internal CPriceWithCurrency Rate;
        internal CPriceWithCurrency Amount;
    }
    internal sealed class CBuyLimitOrder : CLimitOrder
    {
        internal CBuyLimitOrder(CPriceWithCurrency aBuyRate, CPriceWithCurrency aSellAmount, CPriceWithCurrency aSellRate) : base(aBuyRate, aSellAmount)
        {
            this.BuyCurrency = aBuyRate.Currency;
            this.SellAmount = aSellAmount;
            this.BuyRate = aBuyRate;
            this.SellLimitOrder = new CSellLimitOrder(aSellAmount.Currency, aSellRate, this.BuyAmount);
            this.Profit = new CPriceWithCurrency(new CPrice(this.SellLimitOrder.BuyAmount.Amount.Value - aSellAmount.Amount.Value), aSellAmount.Currency);
        }
        internal readonly CCurrency BuyCurrency;
        internal readonly CPriceWithCurrency SellAmount;
        internal CPriceWithCurrency BuyAmount=> this.SellAmount.Exchange(this.BuyRate);
        internal readonly CPriceWithCurrency BuyRate;
        //internal override CAmountWithCurrency Rate => this.BuyRate;// this.Amount;
        internal readonly CSellLimitOrder SellLimitOrder;
        internal readonly CPriceWithCurrency Profit;
        internal decimal ProfitPercent
            => this.SellLimitOrder.Rate.Amount.Value / this.BuyRate.Amount.Invert().Value;

        public override string ToString()
            => "Buy " + this.BuyAmount.ToString()
             + " for " + this.Amount.ToString()
             + " when rate is/below " + this.Rate.Invert(this.SellAmount.Currency).ToString() + "/" + this.BuyCurrency.ToString()
             + ". " + this.SellLimitOrder.ToString()
             + " Make " + this.Profit.ToString() + " profit.";

        internal CPriceWithCurrency ProfitPrice
            => new CPriceWithCurrency(new CPrice(this.SellLimitOrder.Rate.Amount.Value - 1M / this.Rate.Amount.Value), this.BuyCurrency);
        internal string TitleWithRateOnly
            => "Buy for rate <= " + this.Rate.Invert(this.SellAmount.Currency).ToString() + "/" + this.BuyCurrency.ToString()
             + ". " + this.SellLimitOrder.TitleWithRateOnly
             + " Make profit of " + this.ProfitPrice.ToString() + "/" + this.SellAmount.Currency.ToString()
            ;
    }
    internal sealed class CSellLimitOrder : CLimitOrder
    {
        internal CSellLimitOrder(CCurrency aCurrency, CPriceWithCurrency aRate, CPriceWithCurrency aSellAmount) : base(aRate, aSellAmount)
        {
            this.BuyAmount = aSellAmount.Exchange(aRate, aCurrency); // new CAmountWithCurrency(new CAmount(aAmount.Amount.Value * aRate.Amount.Value), aRate.Currency);
        }
        internal CPriceWithCurrency SellAmount => this.Amount;

        public string TitleWithRateOnly
            => "Sell for rate >= " + this.Rate.ToString()
             + ".";

        internal readonly CPriceWithCurrency BuyAmount;
        public override string ToString()
            => "Sell " + this.SellAmount.ToString() 
             + " for " + this.BuyAmount.ToString() 
             + " when rate is/over " + this.Rate.ToString() 
             + "/" + this.SellAmount.Currency.Caption
             + ".";
    }
    internal enum CDirectoryEnum
    {
        Exchanges,
        ExchangeRates,
        Symbols,
        Tracker,

    }

    internal static class CTests
    {
        internal static void Run()
        {
            //Test01();
            Test02();
        }

        internal static void Test01()
        {
            //var aInvestmentPlan = new CInvestmentPlan();
            //var aBuyLimitOrders = aInvestmentPlan.NewBuyLimitOrders();
        }

        private static void Test02()
        {
            //new CCaRequest().GetExchangeRates();
        }
    }



    internal sealed partial class CCbTrader : CViewModel
    {
        public CCbTrader()
        {
            this.SettingsVm.LoadFromPersistentStorage(true);
        }
        private CTracker TrackerM;
        internal CTracker Tracker => CLazyLoad.Get(ref this.TrackerM, () => new CTracker());
        public CTracker VmTracker => this.Tracker;

        protected override void OnDispose()
        {
            base.OnDispose();

            this.Tracker.Dispose();
        }

        #region Settings
        private CSettings SettingsM;
        internal CSettings Settings => CLazyLoad.Get(ref this.SettingsM, () => new CSettings(this));
        private CSettingsVm SettingsVmM;
        internal CSettingsVm SettingsVm => CLazyLoad.Get(ref this.SettingsVmM, this.NewSettingsVm);
        private CSettingsVm NewSettingsVm()
        {
            var aSettingsVm = new CSettingsVm(this);
            return aSettingsVm;
        }
#endregion

        private CExchangeRateHistogram ExchangeRateHistogramM;
        internal CExchangeRateHistogram ExchangeRateHistogram
        {
            get => CLazyLoad.Get(ref this.ExchangeRateHistogramM, this.NewExchangeRateHistogram);
            set
            {
                this.Set(ref this.ExchangeRateHistogramM, value, nameof(this.VmExchangeRateHistogram));
                this.LimitOrdersVms = default;
                this.InvestmentExchangeRateHistogram = default;
            }
        }
        public CExchangeRateHistogram VmExchangeRateHistogram => this.ExchangeRateHistogram;
        private CExchangeRateHistogram NewExchangeRateHistogram()
        {
            var aHistogram1 = this.ExchangeRatePeriodHistogram.NewExchangeRateHistogram(this);
            var aSettings = this.SettingsVm;
            var aInterpolateSetting = aSettings.InterpolateSetting;
            var aHistogram2 
                = aInterpolateSetting.Item1
                ? aHistogram1.Interpolate(aInterpolateSetting.Item2)
                : aHistogram1
                ;
            var aHistogram = aHistogram2;
            return aHistogram;
        }

        //private CPriceWithCurrency InvestAmount => new CPriceWithCurrency(new CPrice(500M), this.Settings.SellCurrency);

        private CExchangeRateHistogram InvestmentExchangeRateHistogramM;
        internal CExchangeRateHistogram InvestmentExchangeRateHistogram
        {
            get => CLazyLoad.Get(ref this.InvestmentExchangeRateHistogramM, () => this.ExchangeRateHistogram);
            set
            {
                this.InvestmentExchangeRateHistogramM = value;
                this.InvestmentPlan = default;
                this.TrendLinePitchNullable = default;
                this.OnPropertyChanged(nameof(this.VmInvestmentExchangeRateHistogram));
                this.OnTrendLinePitchChanged();
            }
        }
        public CExchangeRateHistogram VmInvestmentExchangeRateHistogram => this.InvestmentExchangeRateHistogram;


        private CInvestmentPlan InvestmentPlanM;
        internal CInvestmentPlan InvestmentPlan
        {
            get => CLazyLoad.Get(ref this.InvestmentPlanM, () => new CInvestmentPlan(this.InvestmentExchangeRateHistogram, this.SettingsVm));
            set
            {
                this.InvestmentPlanM = value;
                this.LimitOrdersVms = default;
            }
        }

        private CBuyLimitOrderVms LimitOrdersVmsM;
        internal CBuyLimitOrderVms LimitOrdersVms
        {
            get => CLazyLoad.Get(ref this.LimitOrdersVmsM, () => new CBuyLimitOrderVms(this, this.InvestmentPlan.NewBuyLimitOrders()));
            set
            {
                this.Set(ref this.LimitOrdersVmsM, default, nameof(this.VmLimitOrders));
            }
        }
        public IEnumerable<CBuyLimitOrderVm> VmLimitOrders => this.LimitOrdersVms.Items;
#region InfoProvider
        private ICurrencyInfoProvider InfoProviderM;
        private ICurrencyInfoProvider InfoProvider => CLazyLoad.Get(ref this.InfoProviderM, () => CInfoProviderFactory.Singleton.NewInfoProvider(this));
#endregion
#region SettingsDirectoryInfo
        internal DirectoryInfo SettingsDirectoryInfo
        {
            get => this.Settings.SettingsDirectoryInfo;
            set
            {
                this.Settings.SettingsDirectoryInfo = value;
                this.ExchangeRatePeriodHistogram = default;
            }
        }
#endregion
#region Periods
        private CExchangeRatePeriodHistogram ExchangeRatePeriodHistogramM;
        internal CExchangeRatePeriodHistogram ExchangeRatePeriodHistogram
        {
            get => CLazyLoad.Get(ref this.ExchangeRatePeriodHistogramM, () => CExchangeRatePeriodHistogram.Load(this));
            set
            {
                this.ExchangeRatePeriodHistogramM = value;
                this.ExchangeRateHistogram = default;
                this.LimitOrdersVms = default;
            }
        }

        internal void CompletePeriods(IProgress aProgress,Tuple<DateTime, DateTime> aRange = default)
        {
            var aDone = false;
            var aHistogram = this.ExchangeRatePeriodHistogram;
            var aInfoProvider = this.InfoProvider;
            aProgress.ProgressTitle = "Requesting data from '" + aInfoProvider.Name + "'...";
            var aProgressMaxDone = false;
            var aTotalHours = default(double?);
            var aStartDate = default(DateTime?);
            try
            {
                do
                {
                    var aInvestmentPlan = this.InvestmentPlan;
                    var aDateTime = DateTime.Now;
                    var aQueries 
                        = aRange is object 
                        ? new CExchangeRatePeriodQuery[] { aHistogram.NewCompletionQuery(aRange, aInvestmentPlan.PeriodEnum) }
                        : aHistogram.NewCompletionQueries(aDateTime, aInvestmentPlan).ToArray();
                    if (!aProgressMaxDone
                    && aQueries.Length > 0)
                    {
                        aStartDate = aQueries.First().StartDateTime;
                        var aEndDate = aQueries.Last().EndDateTime;
                        aTotalHours = aEndDate.Subtract(aStartDate.Value).TotalHours;
                    }
                    var aExchangeRatePeriods = aQueries.Select(q =>
                    {
                        aProgress.CheckCancel();
                        var aStartDateTime = q.StartDateTime;
                        aProgress.ProgressSubTitle = aStartDateTime.ToString("G") + "...";
                        var aResult = aInfoProvider.GetExchangeRatePeriods(q);
                        return aResult;
                    }).ToArray().Flatten();
                    if (aExchangeRatePeriods.Count() == 0)
                    {
                        aDone = true;
                    }
                    else
                    {
                        aProgress.ProgressPercent = aExchangeRatePeriods.Last().PeriodEndTime.Subtract(aStartDate.Value).TotalHours / aTotalHours.Value;
                        var aHistogram1 = aHistogram;
                        var aHistgoram2 = aHistogram1.NewCombined(aExchangeRatePeriods);
                        aHistgoram2.Save();
                        aHistogram = aHistgoram2;
                        aDone = aRange is object; // Andernfalls aktuelle daten solange abfragen bis nix mehr kommt.
                    }
                }
                while (!aDone);
            }
            finally
            {
                this.ExchangeRatePeriodHistogram = aHistogram;
            }
        }

        internal void Zoom(DateTime aStartDate, DateTime aEndDate)
        {
            var aHistogram1 = this.ExchangeRateHistogram;
            var aHistogram2 = aHistogram1.NewWithin(aStartDate, aEndDate);
            this.InvestmentExchangeRateHistogram = aHistogram2; 
        }

        internal void Truncate(Tuple<DateTime, DateTime> aDateTimeRange)
        {
            var aOld = this.ExchangeRatePeriodHistogram;
            var aNew = aOld.Truncate(aDateTimeRange);
            aNew.Save();
            this.ExchangeRatePeriodHistogram = aNew;
        }
#endregion
#region TrendLinePitch
        private decimal? TrendLinePitchM;
        private decimal? TrendLinePitchNullable
        {
            get => this.TrendLinePitchM;
            set
            {
                this.TrendLinePitchM = value;
                this.OnPropertyChanged(nameof(this.VmTrendLinePitch));
                this.OnPropertyChanged(nameof(this.VmTrendLinePitchTitle));
                this.LimitOrdersVms.RefreshWeights();
            }
        }

        internal decimal TrendLinePitch
        {
            get => CLazyLoad.Get(ref this.TrendLinePitchM, () => this.InvestmentExchangeRateHistogram.TrendLinePitchCoerced);
            set => this.TrendLinePitchNullable = value;
        }
        public double VmTrendLinePitch
        {
            get => (double)(this.VmWeightTablesEditIsActive ? this.TrendLinePitch : this.InvestmentExchangeRateHistogram.TrendLinePitchCoerced);
            set => this.TrendLinePitch = (decimal)value;
        }
        private void OnTrendLinePitchChanged()
        {
            this.OnPropertyChanged(nameof(this.VmTrendLinePitch));
            this.OnPropertyChanged(nameof(this.VmTrendLinePitchTitle));
            this.OnTrendLinePitchMessageChanged();
        }
        public String VmTrendLinePitchTitle(bool aIncludePropertyName = true) => CExchangeRateHistogram.TrendLinePitchTitle((decimal)this.VmTrendLinePitch, aIncludePropertyName);
        public string VmTrendLinePitchMessage
        {
            get
            {
                var aEditIsEnabled = this.WeightTablesEditIsActive;
                if(aEditIsEnabled)
                {
                    return "Adjust the Trendline-Pitch-Slider to test the characteristic curve.";
                }
                else
                {
                    var aPitch = this.VmTrendLinePitch;
                    var aRising = aPitch > 0;
                    var aFalling = aPitch < 0;
                    var aMsg1 =
                        aPitch == 0
                        ? "TrendLine-Pitch is even: "
                        : aRising
                        ? "TrendLine-Pitch is raising: "
                        : "TrendLine-Pitch is falling: "
                        ;
                    var aMsg2 = aMsg1 + " " + this.VmTrendLinePitchTitle(false);
                    var aMsg3 = aMsg2 + " " + (aRising ? "We should invest." : aFalling ? "We should be carefull." : string.Empty);
                    return aMsg3;
                }
            }
        }
        internal void OnTrendLinePitchMessageChanged()
        {
            this.OnPropertyChanged(nameof(this.VmTrendLinePitchMessage));
        }
        #endregion

    }

        interface IProgress
    {
        void CheckCancel();
        string ProgressTitle { get; set; }
        string ProgressSubTitle { get; set; }
        double ProgressPercent { get; set; }
    }
}
