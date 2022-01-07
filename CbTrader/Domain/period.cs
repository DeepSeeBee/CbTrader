using CbTrader.Settings;
using CbTrader.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CbTrader.Domain
{
    using CDateTimeRange = Tuple<DateTime, DateTime>;

    public enum CPeriodEnum
    {
        Hours12,
        Hours8,
        Hours4,
        Hours2,
        Hours1,
        Minutes30,
        Minutes10,
        Minutes1,
        Seconds20,
        Seconds30,
    }

    internal sealed class CPeriods : Dictionary<CPeriodEnum, TimeSpan>
    {
        private CPeriods()
        {
            this.Add(CPeriodEnum.Hours12,  TimeSpan.FromHours(12));
            this.Add(CPeriodEnum.Hours8,  TimeSpan.FromHours(8));
            this.Add(CPeriodEnum.Hours4,  TimeSpan.FromHours(4));
            this.Add(CPeriodEnum.Hours2,  TimeSpan.FromHours(2));
            this.Add(CPeriodEnum.Hours1,  TimeSpan.FromHours(1));
            this.Add(CPeriodEnum.Minutes30,  TimeSpan.FromMinutes(30));
            this.Add(CPeriodEnum.Minutes10,  TimeSpan.FromMinutes(10));
            this.Add(CPeriodEnum.Minutes1,  TimeSpan.FromMinutes(1));
            this.Add(CPeriodEnum.Seconds20, TimeSpan.FromSeconds(20));
            this.Add(CPeriodEnum.Seconds30, TimeSpan.FromSeconds(30));
        }
        internal static readonly CPeriods Singleton = new CPeriods();
    }

    internal sealed class CExchangeRatePeriodQuery
    {
        internal CExchangeRatePeriodQuery(CPeriodEnum aPeriodEnum, CCurrency aSellCurrency, CCurrency aBuyCurrency, DateTime aStartDateTime, DateTime aEndDateTime)
        {
            this.PeriodEnum = aPeriodEnum;
            this.SellCurrency = aSellCurrency;
            this.BuyCurrency = aBuyCurrency;
            this.StartDateTime = aStartDateTime;
            this.EndDateTime = aEndDateTime;
        }
        internal readonly CPeriodEnum PeriodEnum;
        internal readonly CCurrency SellCurrency;
        internal readonly CCurrency BuyCurrency;
        internal readonly DateTime StartDateTime;
        internal readonly DateTime EndDateTime;
    }

    internal sealed class CExchangeRatePeriod
    {
        internal CExchangeRatePeriod
        (
            CCurrency aSellCurrency, 
            CCurrency aBuyCurrency, 
            DateTime aTimePeriodStart, 
            DateTime aTimePeriodEnd, 
            CPrice aPriceOpen,
            CPrice aPriceClose,
            CPrice aPriceLow,
            CPrice aPriceHigh
        )
        {
            this.SellCurrency = aSellCurrency;
            this.BuyCurrency = aBuyCurrency;
            this.PeriodStartTime = aTimePeriodStart;
            this.PeriodEndTime = aTimePeriodEnd;
            this.OpenPrice = aPriceOpen;
            this.ClosePrice = aPriceClose;
            this.LowPrice = aPriceLow;
            this.HighPrice = aPriceHigh;
        }
        internal readonly CCurrency SellCurrency;
        internal readonly CCurrency BuyCurrency;
        internal readonly DateTime PeriodStartTime;
        internal readonly DateTime PeriodEndTime;
        internal readonly CPrice OpenPrice;
        internal readonly CPrice ClosePrice;
        internal readonly CPrice LowPrice;
        internal readonly CPrice HighPrice;
        internal static readonly string ExchangeRatePeriodXmlElementName = "ExchangeRatePeriod";
        private static readonly string SellCurrencyXmlAttributeName = "SellCurrency";
        private static readonly string BuyCurrencyXmlAttributeName = "BuyCurrency";
        private static readonly string TimePeriodStartXmlAttributeName = "PeriodStartTime";
        private static readonly string TimePeriodEndXmlAttributeName = "PeriodEndTime";
        private static readonly string OpenPriceXmlAttributeName = "OpenPrice";
        private static readonly string ClosePriceXmlAttributeName = "ClosePrice";
        private static readonly string LowPriceXmlAttributeName = "LowPrice";
        private static readonly string HighPriceXmlAttributeName = "HighPrice";
        internal XmlElement NewXmlElement(XmlDocument aXmlDoc)
        {
            var aElement = aXmlDoc.CreateElement(ExchangeRatePeriodXmlElementName);
            aElement.SetAttribute(SellCurrencyXmlAttributeName, this.SellCurrency.Enum.ToString());
            aElement.SetAttribute(BuyCurrencyXmlAttributeName, this.BuyCurrency.Enum.ToString());
            aElement.SetAttribute(TimePeriodStartXmlAttributeName, this.PeriodStartTime.ToString("R"));
            aElement.SetAttribute(TimePeriodEndXmlAttributeName, this.PeriodEndTime.ToString("R"));
            aElement.SetAttribute(OpenPriceXmlAttributeName, this.OpenPrice.ToString());
            aElement.SetAttribute(ClosePriceXmlAttributeName, this.ClosePrice.ToString());
            aElement.SetAttribute(LowPriceXmlAttributeName, this.LowPrice.ToString());
            aElement.SetAttribute(HighPriceXmlAttributeName, this.HighPrice.ToString());
            return aElement;
        }

        internal static CExchangeRatePeriod FromXml(XmlElement aElement)
        {
            var aSellCurrency = CCurrency.Get(aElement.GetAttribute(SellCurrencyXmlAttributeName));
            var aBuyCurrency = CCurrency.Get(aElement.GetAttribute(BuyCurrencyXmlAttributeName));
            var aTimePeriodStart = DateTime.ParseExact(aElement.GetAttribute(TimePeriodStartXmlAttributeName), "R", null);
            var aTimePeriodEnd = DateTime.ParseExact(aElement.GetAttribute(TimePeriodEndXmlAttributeName), "R", null);
            var aOpenPrice = new CPrice(decimal.Parse(aElement.GetAttribute(OpenPriceXmlAttributeName)));
            var aClosePrice = new CPrice(decimal.Parse(aElement.GetAttribute(ClosePriceXmlAttributeName)));
            var aLowPrice = new CPrice(decimal.Parse(aElement.GetAttribute(LowPriceXmlAttributeName)));
            var aHighPrice = new CPrice(decimal.Parse(aElement.GetAttribute(HighPriceXmlAttributeName)));
            var aPeriod = new CExchangeRatePeriod(aSellCurrency, aBuyCurrency, aTimePeriodStart, aTimePeriodEnd, aOpenPrice, aClosePrice, aLowPrice, aHighPrice);
            return aPeriod;
        }
    }

    internal sealed class CExchangeRatePeriodHistogram
    {
        internal CExchangeRatePeriodHistogram(CCbTrader aTrader, CCurrency aSellCurrency, CCurrency aBuyCurrency, CExchangeRatePeriod[] aItems)
        {
            this.Trader = aTrader;
            this.SellCurrency = aSellCurrency;
            this.BuyCurrency = aBuyCurrency;
            this.Items = aItems;
        }
        private readonly CCbTrader Trader;
        internal CSettings Settings => this.Trader.Settings;
        internal readonly CCurrency SellCurrency;
        internal readonly CCurrency BuyCurrency;
        internal readonly CExchangeRatePeriod[] Items;

        private static readonly string ExchangeRatePeriodsElementName = "ExchangeRatePeriods";
        private static readonly string SellCurrencyXmlAttributeName = "SellCurrency";
        private static readonly string BuyCurrencyXmlAttributeName = "BuyCurrency";
        internal void Save()
        {
            var aXmlDoc = new XmlDocument();
            var aRootElement = aXmlDoc.CreateElement(ExchangeRatePeriodsElementName);
            var aSellCurrency = this.SellCurrency;
            var aBuyCurrency = this.BuyCurrency;
            aRootElement.SetAttribute(SellCurrencyXmlAttributeName, aSellCurrency.Enum.ToString());
            aRootElement.SetAttribute(BuyCurrencyXmlAttributeName, aBuyCurrency.Enum.ToString());
            var aElements = this.Items.Select(i => i.NewXmlElement(aXmlDoc)).ToArray();
            foreach (var aElement in aElements)
            {
                aRootElement.AppendChild(aElement);
            }
            aXmlDoc.AppendChild(aRootElement);
            var aSettings = this.Settings;
            var aFileInfo = aSettings.GetExchangeRatePeriodsFileInfo();
            aXmlDoc.Save(aFileInfo.FullName);
        }

        internal static CExchangeRatePeriodHistogram Load(CCbTrader aTrader)
        {
            var aSettings = aTrader.Settings;
            var aFileInfo = aSettings.GetExchangeRatePeriodsFileInfo();
            aFileInfo.Refresh();
            if (aFileInfo.Exists)
            {
                var aDoc = new XmlDocument();
                aDoc.Load(aFileInfo.FullName);
                var aHistogram = CExchangeRatePeriodHistogram.FromXml(aTrader, aDoc);
                return aHistogram;
            }
            else
            {
                var aBuyCurrency = aSettings.BuyCurrency;
                var aSellCurrency = aSettings.SellCurrency;
                var aHistogram = new CExchangeRatePeriodHistogram(aTrader, aSellCurrency, aBuyCurrency, new CExchangeRatePeriod[] { });
                return aHistogram;
            }
        }

        private static CExchangeRatePeriodHistogram FromXml(CCbTrader aTrader, XmlDocument aDoc)
        {
            var aRootElement = (XmlElement)aDoc.SelectSingleNode(ExchangeRatePeriodsElementName);
            var aSellCurrencyName = aRootElement.GetAttribute(SellCurrencyXmlAttributeName);
            var aBuyCurrencyName = aRootElement.GetAttribute(BuyCurrencyXmlAttributeName);
            var aSellCurrency = CCurrency.Get(aSellCurrencyName);
            var aBuyCurreny = CCurrency.Get(aBuyCurrencyName);
            var aItemsElements = aRootElement.SelectNodes(CExchangeRatePeriod.ExchangeRatePeriodXmlElementName).OfType<XmlElement>();
            var aItems = aItemsElements.Select(el => CExchangeRatePeriod.FromXml(el)).ToArray();
            var aHistogram = new CExchangeRatePeriodHistogram(aTrader, aSellCurrency, aBuyCurreny, aItems);
            return aHistogram;
        }

        internal CExchangeRatePeriod Oldest => this.Items.First();
        internal CExchangeRatePeriod Newest => this.Items.Last();
        internal bool ContainsOneOrMoreElements => this.Items.Length > 0;
        internal CExchangeRatePeriodQuery NewCompletionQuery(CDateTimeRange aDateTimeRange, CPeriodEnum aPeriodEnum)
        {
            var aSellCurrency = this.SellCurrency;
            var aBuyCurrency = this.BuyCurrency;
            var aQuery = new CExchangeRatePeriodQuery(aPeriodEnum, aSellCurrency, aBuyCurrency, aDateTimeRange.Item1, aDateTimeRange.Item2);
            return aQuery;
        }
        internal CExchangeRatePeriodQuery[] NewCompletionQueries(DateTime aStartDateTime, CInvestmentPlan aInvestmentPlan)
        {
            var aPeriodEnum = aInvestmentPlan.PeriodEnum;
            var aPeriodTimeSpan = CPeriods.Singleton[aPeriodEnum];
            var aLookbackTimeSpan = aInvestmentPlan.LookbackTimespan;
            var aSellCurrency = this.SellCurrency;
            var aBuyCurrency = this.BuyCurrency;

            if (this.ContainsOneOrMoreElements)
            {
                var aQueries = new List<CExchangeRatePeriodQuery>();
                var aOldest = this.Oldest;
                var aOldestDate = aOldest.PeriodStartTime;
                var aNewest = this.Newest;
                var aNewestDate = aNewest.PeriodEndTime;
                var aEarliestStartPeriod = aStartDateTime.Subtract(aLookbackTimeSpan);
                var aFillFront = aEarliestStartPeriod.Add(aPeriodTimeSpan).CompareTo(aOldestDate) < 0;
                if (aFillFront)
                {
                    var aStartPeriod = aOldestDate;
                    do
                    {
                        aStartPeriod = aStartPeriod.Subtract(aPeriodTimeSpan);
                    }
                    while (aStartPeriod.CompareTo(aEarliestStartPeriod) > 0);
                    var aEndPeriod = aOldestDate;
                    var aQuery = new CExchangeRatePeriodQuery(aPeriodEnum, aSellCurrency, aBuyCurrency, aStartPeriod, aEndPeriod);
                    aQueries.Add(aQuery);
                }
                var aNewestExpected = aStartDateTime.Subtract(aPeriodTimeSpan);
                var aFillBack = aNewestExpected.CompareTo(aNewestDate) > 0;
                if(aFillBack)
                {
                    var aPeriodEndDate = aNewestDate;
                    do
                    {
                        aPeriodEndDate = aPeriodEndDate.Add(aPeriodTimeSpan);
                    }
                    while (aPeriodEndDate.Add(aPeriodTimeSpan).CompareTo(aStartDateTime) < 0);
                    var aQuery = new CExchangeRatePeriodQuery(aPeriodEnum, aSellCurrency, aBuyCurrency, aNewestDate, aPeriodEndDate);
                    aQueries.Add(aQuery);
                }
                return aQueries.ToArray();
            }
            else
            {
                var aTime1 = aStartDateTime.Subtract(aLookbackTimeSpan);
                var aTime2 = aStartDateTime;
                var aQuery = new CExchangeRatePeriodQuery(aPeriodEnum, aSellCurrency, aBuyCurrency, aTime1, aTime2);
                var aQuerys = new CExchangeRatePeriodQuery[] { aQuery };
                return aQuerys;
            }
        }

        internal CExchangeRatePeriodHistogram NewCombined(IEnumerable<CExchangeRatePeriod> aExchangeRatePeriods)
        {
            var aItems1 = this.Items;
            var aItems2 = aExchangeRatePeriods;
            var aItems3 = aItems1.Concat(aItems2);
            var aItems4 = aItems3.OrderBy(i => i.PeriodStartTime);
            var aItems5 = aItems4.ToArray();
            var aHistogram = new CExchangeRatePeriodHistogram(this.Trader, this.SellCurrency, this.BuyCurrency, aItems5);
            return aHistogram;
        }

        internal CExchangeRateHistogram NewExchangeRateHistogram(CCbTrader aTrader)
        {
            var aNewItems = new Func<CExchangeRatePeriod, CExchangeRate[]>(aPeriod =>
            {
                var aBuyCurrency = this.BuyCurrency;
                var aOpenAmount = new CPriceWithCurrency(aPeriod.OpenPrice, aBuyCurrency);                
                var aStartTime = aPeriod.PeriodStartTime;
                var aEndTime = aPeriod.PeriodEndTime;
                var aSellCurrency = aPeriod.SellCurrency;
                var aFirst = new CExchangeRate(aStartTime, aSellCurrency, aOpenAmount);
                var aSecond = new CExchangeRate(aEndTime, aSellCurrency, aOpenAmount);
                var aExchangeRates = new CExchangeRate[] { aFirst, aSecond };
                return aExchangeRates;
            });
            var aItems1 = this.Items.Select(i => aNewItems(i)).Flatten().ToArray();
            var aItems2 = new List<CExchangeRate>(aItems1.Length);
            var aPreviousItem = default(CExchangeRate);
            foreach(var aItem in aItems1)
            {
                if(aPreviousItem is object)
                {
                    var aDateTimeEqual = aPreviousItem.DateTime == aItem.DateTime;
                    if(aDateTimeEqual)
                    {
                        //var aInterPolated = CExchangeRate.Interpolate(aPreviousItem, aItem);
                        //aItems2.Add(aInterPolated);
                        //aPreviousItem = aInterPolated;
                    }
                    else
                    {
                        aItems2.Add(aItem);
                        aPreviousItem = aItem;
                    }
                }
                else
                {
                    aItems2.Add(aItem);
                    aPreviousItem = aItem;
                }
            }
            var aItems3 = aItems2.OrderBy(i => i.DateTime);
            var aHistogram1 = new CExchangeRateHistogram(aTrader, this.SellCurrency, this.BuyCurrency, aItems3.ToArray());
            //var aInterpolateInterval = TimeSpan.FromMinutes(30);
            //var aHistogram2 = aHistogram1.Interpolate(aInterpolateInterval);
            var aHistogram = aHistogram1;
            return aHistogram;
        }

        internal CExchangeRatePeriodHistogram Truncate(CDateTimeRange aDateTimeRange)
        {
            var aOldItems = this.Items;
            var aNewItems = aOldItems.Where(i => ! (aDateTimeRange.Contains(i.PeriodStartTime) && aDateTimeRange.Contains(i.PeriodEndTime)));
            var aTruncated = new CExchangeRatePeriodHistogram(this.Trader, this.SellCurrency, this.BuyCurrency, aNewItems.ToArray());
            return aTruncated;
        }
    }
}
