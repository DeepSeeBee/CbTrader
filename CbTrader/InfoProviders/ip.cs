using CbTrader.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbTrader.InfoProviders
{
    enum CInfoProviderEnum
    {
        CoinApi
    }

    //internal sealed class CCurrencyMap
    //{
    //    internal CCurrencyMap()
    //    {
    //        this.Add(CInfoProviderEnum.CoinApi, CCurrencyEnum.Euro, "EUR");
    //        this.Add(CInfoProviderEnum.CoinApi, CCurrencyEnum.Ripple, "XRP");
    //    }
    //    private Dictionary<CInfoProviderEnum, Dictionary<CCurrencyEnum, string>> Dic = new Dictionary<CInfoProviderEnum, Dictionary<CCurrencyEnum, string>>();

    //    internal void Add(CInfoProviderEnum aInfoProviderEnum, CCurrencyEnum aCurrencyEnum, string aIdentifier)
    //    {
    //        this.GetCurrencyDic(aInfoProviderEnum).Add(aCurrencyEnum, aIdentifier);
    //    }

    //    private Dictionary<CCurrencyEnum, string> GetCurrencyDic(CInfoProviderEnum aInfoProviderEnum)
    //    {
    //        if (this.Dic.ContainsKey(aInfoProviderEnum))
    //        {
    //            return this.Dic[aInfoProviderEnum];
    //        }
    //        else
    //        {
    //            var aCurrencyDic = new Dictionary<CCurrencyEnum, string>();
    //            this.Dic.Add(aInfoProviderEnum, aCurrencyDic);
    //            return aCurrencyDic;
    //        }
    //    }
    //}

    internal interface ICurrencyInfoProvider
    {
        string Name { get; }
        int RequestsPerDay { get; }
        CExchangeRate GetCurrentExchangeRate(CCurrency aFromCurrency, CCurrency aToCurrency);
        CExchangeRatePeriod[] GetExchangeRatePeriods(CExchangeRatePeriodQuery aQuery);
        IEnumerable<CPeriodEnum> PeriodEnums { get; }

    }

    internal sealed class CInfoProviderFactory
    {
        internal static readonly CInfoProviderFactory Singleton = new CInfoProviderFactory();
        internal ICurrencyInfoProvider NewInfoProvider(CCbTrader aTrader)
            => new CoinApi.CCaInfoProvider(aTrader);
    }
}
