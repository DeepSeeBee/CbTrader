using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

namespace CbTrader.Utils
{
    using CDateTimeRange = Tuple<DateTime, DateTime>;

    //internal sealed class CExchangeRate
    //{
    //    internal CExchangeRate(DateTime aDateTime, CCurrency aSellCurrency, CPriceWithCurrency aBuyAmount)
    //    {
    //        this.DateTime = aDateTime;
    //        this.SellCurrency = aSellCurrency;
    //        this.BuyAmount = aBuyAmount;
    //    }
    //    internal static CExchangeRate FromXml(XmlElement aXmlElement)
    //    {
    //        var aDateTimeString = aXmlElement.GetAttribute(DateTimeAttributeName);
    //        var aDateTime = DateTime.ParseExact(aDateTimeString, DateTimeFormat, null);
    //        var aSellCurrencyText = aXmlElement.GetAttribute(SellCurrencyAttributeName);
    //        var aSellCurrency = CCurrency.Parse(aSellCurrencyText);
    //        var aAmountWithCurrency = CPriceWithCurrency.FromXml(aXmlElement);
    //        var aExchangeRate = new CExchangeRate(aDateTime, aSellCurrency, aAmountWithCurrency);
    //        return aExchangeRate;
    //    }
    //    internal readonly DateTime DateTime;
    //    internal readonly CCurrency SellCurrency;
    //    private static readonly string SellCurrencyAttributeName = "SellCurrency";
    //    private static readonly string DateTimeAttributeName = "DateTime";
    //    internal readonly CPriceWithCurrency BuyAmount;
    //    internal static readonly string XmlElementName = "ExchangeRate";
    //    private static readonly string DateTimeFormat = "R";
    //    internal static bool BuyCurrencyIsEqual(CCurrency aBuyCurrency, params CExchangeRate[] aRates)
    //    {
    //        foreach (var aRate in aRates)
    //        {
    //            if (aRate.BuyAmount.Currency.Enum != aBuyCurrency.Enum)
    //                return false;
    //        }
    //        return true;
    //    }
    //    internal static bool SellCurrencyIsEqual(CCurrency aSellCurrency, params CExchangeRate[] aRates)
    //    {
    //        foreach (var aRate in aRates)
    //        {
    //            if (aRate.SellCurrency.Enum != aSellCurrency.Enum)
    //                return false;
    //        }
    //        return true;
    //    }
    //    internal static bool CurrenciesAreEqual(CCurrency aSellCurrency, CCurrency aBuyCurrency, params CExchangeRate[] aRates)
    //    {
    //        var aEqual = SellCurrencyIsEqual(aSellCurrency, aRates)
    //                  && BuyCurrencyIsEqual(aBuyCurrency, aRates)
    //                  ;
    //        return aEqual;
    //    }
    //    internal void ThrowIfNotCurrenciesAreEqual(params CExchangeRate[] aRates)
    //    {
    //        if (!CurrenciesAreEqual(this.SellCurrency, this.BuyAmount.Currency, aRates))
    //        {
    //            throw new ArgumentException(this.GetType().Name + ": Currencies missmatch.");
    //        }
    //    }
    //    internal static CExchangeRate Interpolate(CExchangeRate aLeft, CExchangeRate aRight)
    //    {
    //        aLeft.ThrowIfNotCurrenciesAreEqual(aRight);
    //        var aTime1 = aLeft.DateTime;
    //        var aTime2 = aRight.DateTime;
    //        var aMinTime = aTime1.Min(aTime2);
    //        var aMaxTime = aTime1.Max(aTime2);
    //        var aTime3 = aMinTime.Add(aMaxTime.Subtract(aMinTime));
    //        var aValue1 = aLeft.BuyAmount.Amount.Value;
    //        var aValue2 = aRight.BuyAmount.Amount.Value;
    //        var aMinValue = Math.Min(aValue1, aValue2);
    //        var aMaxValue = Math.Max(aValue1, aValue2);
    //        var aDiff = aMaxValue - aMinValue;
    //        var aValue3 = aMinValue + aDiff / 2M;
    //        var aInterpolatedTime = aTime3;
    //        var aInterpolatedValue = aValue3;
    //        var aInterpolatedBuyPrice = new CPriceWithCurrency(new CPrice(aInterpolatedValue), aLeft.BuyAmount.Currency);
    //        var aInterpolatedRate = new CExchangeRate(aInterpolatedTime, aLeft.SellCurrency, aInterpolatedBuyPrice);
    //        return aInterpolatedRate;
    //    }

    //    internal string Title =>
    //        this.BuyAmount.Invert().ToString() + "/" + this.SellCurrency.ToString();

    //    internal string ToXmlString()
    //    {
    //        var aDoc = new XmlDocument();
    //        var aEl = aDoc.CreateElement(XmlElementName);
    //        aEl.SetAttribute(DateTimeAttributeName, this.DateTime.ToString(DateTimeFormat));
    //        aEl.SetAttribute(SellCurrencyAttributeName, this.SellCurrency.Enum.ToString());
    //        this.BuyAmount.SetAttributes(aEl);
    //        var aXmlString = aEl.OuterXml;
    //        return aXmlString;
    //    }
    //}

    internal static class CExtensions
    {
    
        internal static double Interpolate(this double v, double r_min, double r_max, double w_min, double w_max)
        {
            var r_d = r_max - r_min;
            var f = (v - r_min) / r_d;
            var w_d = w_max - w_min;
            var i = w_min + w_d * f;
            return i;
        }
        internal static bool Contains(this CDateTimeRange r, DateTime d)
            => d.CompareTo(r.Item1) >= 0 && d.CompareTo(r.Item2) <= 0;  

        internal static T GetVisualParentNullable<T> (this DependencyObject aChild)
        {
            if (aChild is T)
                return (T)(object)aChild;
            else
                return VisualTreeHelper.GetParent(aChild).GetVisualParentNullable<T>();
        }

        internal static T GetVisualParent<T>(this DependencyObject aChild)
        {
            var aParent = aChild.GetVisualParentNullable<T>();
            if (aParent is object)
                return aParent;
            throw new Exception("Visual parent of type '" + typeof(T).Name + "' not found.");
        }

        internal static IEnumerable<int> IndexOf<T>(this IEnumerable<T> aItems, T aItem)
        {
            return Enumerable.Range(0, aItems.Count()).Select(i2 => new Tuple<int, T>(i2, aItems.ElementAt(i2))).Where(t => object.Equals(t.Item2, aItem)).Select(t => t.Item1);
        }

        internal static int? IndexOfNullable<T>(this IEnumerable<T> aItems, T aItem)
        {
            var aIndexes = aItems.IndexOf(aItem).ToArray();
            var aIndex = aIndexes.Length == 0 ? default(int?) : aIndexes.Single();
            return aIndex;
        }
        internal static Size GetTextSize(this TextBlock aTextBlock)
        {
            var aText = aTextBlock.Text;
            var aTypeFace = new Typeface(aTextBlock.FontFamily, aTextBlock.FontStyle, aTextBlock.FontWeight, aTextBlock.FontStretch);
            var aFormatedText = new FormattedText(aText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, aTypeFace, aTextBlock.FontSize, aTextBlock.Foreground, new NumberSubstitution(), 1);
            var aTextSize = new Size(aFormatedText.Width, aFormatedText.Height);
            return aTextSize;
        }

        internal static void CatchUnexpected(this Exception aExc)
        {
            aExc.ShowMessageBox();
        }

        internal static DateTime Min(this DateTime aTime1, DateTime aTime2)
            => aTime1.CompareTo(aTime2) < 0 ? aTime1 : aTime2;
        internal static DateTime Max(this DateTime aTime1, DateTime aTime2)
            => aTime1.CompareTo(aTime2) > 0 ? aTime1 : aTime2;

        internal static void InvokeWithExceptionHandler(this Action aAction, Action<Exception> aOnExc)
        {
            try
            {
                aAction();
            }
            catch (Exception aExc)
            {
                aOnExc(aExc);
            }
        }
        internal static void ShowMessageBox(this Exception aExc)
            => System.Windows.MessageBox.Show(aExc.Message, "CbTrader", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        internal static void InvokeWithExceptionMessageBox(this Action aAction)
            => aAction.InvokeWithExceptionHandler(ShowMessageBox);

        internal static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> aItemss)
            => from aItems in aItemss
               from aItem in aItems
               select aItem;
        internal static IEnumerable<T> Flatten<T>(this IEnumerable<T[]> aItemss)
            => (from aItems in aItemss select aItems.AsEnumerable<T>()).Flatten();

        internal static int? FindMostIndex<T>(this T[] aTs, Func<Tuple<T, T>, bool> aGetIsMore)
        {
            var aMost = default(Tuple<int, T>);
            foreach (var aIdx in Enumerable.Range(0, aTs.Length))
            {
                var aT = aTs[aIdx];
                if (aMost is object)
                {
                    if (aGetIsMore(new Tuple<T, T>(aT, aMost.Item2)))
                    {
                        aMost = new Tuple<int, T>(aIdx, aT);
                    }
                }
                else
                {
                    aMost = new Tuple<int, T>(aIdx, aT);
                }
            }
            var aMostIdx = aMost is object ? aMost.Item1 : default(int?);
            return aMostIdx;
        }
        internal static int? FindMaxIndex<T, TValue>(this T[] aTs, Func<T, TValue> aGetValue) where TValue : IComparable
            => aTs.FindMostIndex<T>(vs => aGetValue(vs.Item1).CompareTo(aGetValue(vs.Item2)) > 0);
        internal static int? FindMinIndex<T, TValue>(this T[] aTs, Func<T, TValue> aGetValue) where TValue : IComparable
            => aTs.FindMostIndex<T>(vs => aGetValue(vs.Item1).CompareTo(aGetValue(vs.Item2)) < 0);
    }


}
