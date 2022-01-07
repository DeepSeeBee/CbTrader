using CbTrader.Domain;
using CbTrader.Settings;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CbTrader.InfoProviders.CoinApi
{
    // https://docs.coinapi.io/#md-docs

    using CPeriodTuple = Tuple<CPeriodEnum, string>;
    using CInvertAndSymbol = Tuple<bool, string>;

    internal sealed class CPeriodTuples : Dictionary<CPeriodEnum, CPeriodTuple>
    {
        internal CPeriodTuples()
        {
            this.Add(Hours12);
            this.Add(Hours8);
            this.Add(Hours4);
            this.Add(Hours2);
            this.Add(Hours1);
            this.Add(Minutes30);
            this.Add(Minutes1);
            this.Add(Minutes10);
            this.Add(Seconds20);
            this.Add(Seconds30);
        }

        internal void Add(CPeriodTuple aPeriodTuple)
        {
            this.Add(aPeriodTuple.Item1, aPeriodTuple);
        }
        internal static readonly CPeriodTuples Singleton = new CPeriodTuples();
        internal readonly CPeriodTuple Hours12 = new CPeriodTuple(CPeriodEnum.Hours12, "12HRS");
        internal readonly CPeriodTuple Hours8 = new CPeriodTuple(CPeriodEnum.Hours8, "8HRS");
        internal readonly CPeriodTuple Hours4 = new CPeriodTuple(CPeriodEnum.Hours4, "4HRS");
        internal readonly CPeriodTuple Hours2 = new CPeriodTuple(CPeriodEnum.Hours2, "2HRS");
        internal readonly CPeriodTuple Hours1 = new CPeriodTuple(CPeriodEnum.Hours1, "1HRS");
        internal readonly CPeriodTuple Minutes30 = new CPeriodTuple(CPeriodEnum.Minutes30, "30MIN");
        internal readonly CPeriodTuple Minutes1 = new CPeriodTuple(CPeriodEnum.Minutes1, "1MIN");
        internal readonly CPeriodTuple Minutes10 = new CPeriodTuple(CPeriodEnum.Minutes10, "10MIN");
        internal readonly CPeriodTuple Seconds20 = new CPeriodTuple(CPeriodEnum.Seconds20, "20SEC");
        internal readonly CPeriodTuple Seconds30 = new CPeriodTuple(CPeriodEnum.Seconds30, "30SEC");
    }
    internal sealed class CCaRequest
    {
        internal CCaRequest(CSettingsVm aSettingsVm)
        {
            this.SettingsVm = aSettingsVm;
        }

        private readonly CSettingsVm SettingsVm;
        internal readonly string Url = "http://rest.coinapi.io/";        
        internal readonly string ApiKeyHeaderName="X-CoinAPI-Key";
        private string ApiKey => this.SettingsVm.CoinApiKey;
        internal string GetCustomHeader(string aName, string aValue)
            => aName + ":" + " " + aValue;
        internal string CustomAuthorizationHeader => this.GetCustomHeader("X-CoinAPI-Key", this.ApiKey);
        private void AddHeaders(HttpRequestHeaders aHeaders)
        {
            aHeaders.Add("Accept", "application/json");
            aHeaders.Add(this.ApiKeyHeaderName, this.ApiKey);
        }
        private Exception NewExc(HttpStatusCode aStatusCode)
        => new Exception("CoinBaseApi.Request returned status code " + aStatusCode.ToString() + " (" + ((int)aStatusCode).ToString() + ").");

        private void ThrowOnDemand(HttpStatusCode aStatusCode)
        {
            if((int)aStatusCode == 401)
            {
                throw new Exception("CoinApi-Service reports: 401: Unauthorized: Your API key is wrong");
            }
            else if ((int)aStatusCode == 429)
            {
                throw new Exception("CoinApi-Service reports: 429: Too many requests: You have exceeded your API key rate limits");
            }
            else if(aStatusCode != HttpStatusCode.OK)
            {
                throw this.NewExc(aStatusCode);
            }
        }
        private void ThrowOnDemand(HttpResponseMessage aResponseMessage)
        {
            this.ThrowOnDemand(aResponseMessage.StatusCode);
        }

        private void SaveResponse(string aResponse, DirectoryInfo aDirectoryInfo)
        {
            var aSettings = this.Settings;
            var aFileInfo = aSettings.NewFileInfo(aDirectoryInfo, DateTime.Now, ".txt");
            aFileInfo.Directory.Create();
            File.WriteAllText(aFileInfo.FullName, aResponse);
        }
        private void SaveHttpResponse(HttpResponseMessage aHttpResponseMessage)
        {
            var aResponseContent = aHttpResponseMessage.Content;
            var aReadResponseContentTask = aResponseContent.ReadAsStringAsync();
            var aString = aReadResponseContentTask.GetAwaiter().GetResult();

        }

        private RestClient RestClientM;
        private RestClient RestClient => CLazyLoad.Get(ref this.RestClientM, () => new RestClient());

        private RestRequest NewRestRequest()
        {
            var aRestRequest = new RestRequest(Method.GET);
            aRestRequest.AddHeader(this.ApiKeyHeaderName, this.ApiKey);
            return aRestRequest;
        }


        private readonly string Url_Base = "https://rest.coinapi.io/";
        private string Url_Exchanges => this.Url_Base + "v1/exchanges";
        private string Url_ExchangeRates => this.Url_Base + "v1/exchangerate";
        private string Url_Icons => this.Url_Base + "v1 / assets/icons/";
        private string GetUrlIcons(int aSize) => this.Url_Icons + aSize.ToString();
        private string GetExchangeRatesUrl(string aFromCurrency, string aToCurrency)
            => this.Url_ExchangeRates + "/" + aFromCurrency + "/" + aToCurrency;
        private string GetExchangeRatesUrl(CCurrency aFrom, CCurrency aTo)
            => this.GetExchangeRatesUrl(this.GetCaCurrency(aFrom), this.GetCaCurrency(aTo));
        private string GetCaCurrency(CCurrency aCurrency)
            => CCaInfoProvider.CurrencyIdentifierDic[aCurrency.Enum];
        #region Exchanges
        private string Url_Exchanges_Base => this.Url_Base + "";


        #endregion
        #region Symbols
        private string Url_Symbols => this.Url_Base + "v1/symbols";
        private string GetSymbols()
            => this.Get(this.Url_Symbols, this.Settings.CoinApiSymbolsDirectoryInfo);
        private IEnumerable<CInvertAndSymbol> GetHardcodedExchangeSymbols()
        {
            // Call 'GetSymbols'
            // Check the saved JSON.Result in folder directory: this.Settings.CoinApiSymbolsDirectoryInfo
            yield return new CInvertAndSymbol(true, "BITSTAMP_SPOT_XRP_EUR");



            //yield return "KRAKEN_SPOT_EUR_XRP";
            //yield return "THEROCKTRADING_SPOT_EUR_XRP";     // gerundet auf 2 nachkommastellen.
            //yield return "INDOEX_SPOT_EUR_XRP";
            //yield return "GATEHUB_SPOT_EUR_XRP";
            //yield return "SISTEMKOIN_SPOT_XRP_EURO";
        }
        private CInvertAndSymbol GetExchangeSymbol(CCurrency aFromCurrency, CCurrency aToCurrency)
        { 
            var aInvertAndSymbol = this.GetHardcodedExchangeSymbols().First();
            var aInvert = aInvertAndSymbol.Item1;
            var aSymbol = aInvertAndSymbol.Item2;
            var aCurrencies1 = new string[] { this.GetCaCurrency(aFromCurrency) , this.GetCaCurrency(aToCurrency) };
            var aCurrencies2 = aInvert ? aCurrencies1.Reverse() : aCurrencies1;
            var aExpected = string.Join("_", aCurrencies2);
            if(aSymbol.Contains(aExpected))
            {
                return aInvertAndSymbol;
            }
            else
            {
                throw new Exception("No exchange id hard coded for these currencies: " + aExpected);
            }
        }
        #endregion

        private string Get(string aUrl, DirectoryInfo aResponseDirectoryInfo, params Parameter[] aParameters)
        {
            var aClient = new RestClient(aUrl);
            var aRequest = this.NewRestRequest();
            foreach(var aParameter in aParameters)
            {
                aRequest.AddParameter(aParameter);
            }
            var aResponse = aClient.Execute(aRequest);
            this.ThrowOnDemand(aResponse.StatusCode);
            var aContent = aResponse.Content;
            this.SaveResponse(aContent, aResponseDirectoryInfo);
            return aContent;

        }

        internal string GetExchanges()
            => this.Get(this.Url_Exchanges, this.Settings.CoinApiExchangesDirectoryInfo);

        internal string GetExchangeRate(CCurrency aFromCurrency, CCurrency aToCurrency)
            => this.Get(this.GetExchangeRatesUrl(aFromCurrency, aToCurrency), this.Settings.GetCoinApiExchangeRateDirectoryInfo(aFromCurrency, aToCurrency));

        internal string GetExchangeRates()
            => this.GetExchangeRate(this.Settings.SellCurrency, this.Settings.BuyCurrency);

        #region HistoricalData#
        private string Url_HistoricalData_Base => this.Url_Base + "v1/ohlcv/";
        private string GetDateTimeIso8601(DateTime aDateTime)
            => aDateTime.ToString("yyyy-MM-dd") + "T" + aDateTime.ToString("HH:mm:ss");

        private CInvertAndSymbol Url_HistoricalData(CExchangeRatePeriodQuery aQuery)
        {
            // ohlcv/BITSTAMP_SPOT_BTC_USD/history?period_id=1MIN&time_start=2016-01-01T00:00:00");
            var aSellCurrency = aQuery.SellCurrency;
            var aBuyCurrency = aQuery.BuyCurrency;
            var aTimeStart = aQuery.StartDateTime;
            var aTimeEnd = aQuery.EndDateTime;        
            var aTimeStartIso8601 = this.GetDateTimeIso8601(aTimeStart);
            var aTimeEndIso8601 = this.GetDateTimeIso8601(aTimeEnd);
            var aStringBuilder = new StringBuilder();
            var aPeriod = CPeriodTuples.Singleton[aQuery.PeriodEnum].Item2;
            var aInvertAndSymbol = this.GetExchangeSymbol(aSellCurrency, aBuyCurrency);
            var aInvert = aInvertAndSymbol.Item1;
            var aSymbol = aInvertAndSymbol.Item2;
            aStringBuilder.Append(this.Url_HistoricalData_Base);
            aStringBuilder.Append(aSymbol);
            aStringBuilder.Append("/");
            aStringBuilder.Append("history?");
            aStringBuilder.Append("period_id");
            aStringBuilder.Append("=");
            aStringBuilder.Append(aPeriod);
            aStringBuilder.Append("&");
            aStringBuilder.Append("time_start");
            aStringBuilder.Append("=");
            aStringBuilder.Append(aTimeStartIso8601);
            aStringBuilder.Append("&");
            aStringBuilder.Append("time_end");
            aStringBuilder.Append("=");
            aStringBuilder.Append(aTimeEndIso8601);
            var aUrl = aStringBuilder.ToString();
            var aInvertAndUrl = new CInvertAndSymbol(aInvert, aUrl);
            return aInvertAndUrl;
        }
        internal CInvertAndSymbol GetHistoricalData(CExchangeRatePeriodQuery aQuery)
        {
            var aInvertAndUrl = this.Url_HistoricalData(aQuery);
            var aInvert = aInvertAndUrl.Item1;
            var aUrl = aInvertAndUrl.Item2;
            var aResponse = this.Get(aUrl, this.Settings.GetCoinApiHistoricalDataDirectory(aQuery.SellCurrency, aQuery.BuyCurrency));            
            var aInvertAndResponse = new CInvertAndSymbol(aInvert, aResponse);
            return aInvertAndResponse;
        }
        #endregion

        internal CSettings Settings => this.SettingsVm.Settings; 

        //internal void Run()
        //{
        //    var aFileName = Guid.NewGuid().ToString();
        //    var aUrl = this.Url;
        //    var aHttpRequestMessage = new HttpRequestMessage(HttpMethod.Get, aUrl);
        //    this.AddHeaders(aHttpRequestMessage.Headers);
        //    var aHttpClient = new HttpClient();
        //    var aTask = aHttpClient.SendAsync(aHttpRequestMessage);
        //    var aAwaiter = aTask.GetAwaiter();
        //    var aHttpResponseMessage = aAwaiter.GetResult();
        //    this.ThrowOnDemand(aHttpResponseMessage);
        //    this.SaveHttpResponse(aHttpResponseMessage);
        //}
    }
    internal sealed class CCaInfoProvider : ICurrencyInfoProvider
    {
        internal CCaInfoProvider(CCbTrader aTrader)
        {
            this.Trader = aTrader;
        }
        private readonly CCbTrader Trader;
        string ICurrencyInfoProvider.Name => "www.coinapi.io";

        private static Dictionary<CCurrencyEnum, string> CurrencyIdentifierDicM;
        internal static Dictionary<CCurrencyEnum, string> CurrencyIdentifierDic => CLazyLoad.Get(ref CurrencyIdentifierDicM, NewCurrencyIdentifierDic);
        private static Dictionary<CCurrencyEnum, string> NewCurrencyIdentifierDic()
        {
            var aDic = new Dictionary<CCurrencyEnum, string>();
            aDic.Add(CCurrencyEnum.EUR, "EUR");
            aDic.Add(CCurrencyEnum.XRP, "XRP");
            return aDic;
        }

        private CPeriodTuples PeriodTuples => CPeriodTuples.Singleton;
        IEnumerable<CPeriodEnum> ICurrencyInfoProvider.PeriodEnums => this.PeriodTuples.Select(p => p.Key);


        CExchangeRate ICurrencyInfoProvider.GetCurrentExchangeRate(CCurrency aFromCurrency, CCurrency aToCurrency)
        {
            var aRequest = new CCaRequest(this.Trader.SettingsVm);
            var aExchangeRateString = aRequest.GetExchangeRate(aFromCurrency, aToCurrency);
            var aExchangeRateJson = JObject.Parse(aExchangeRateString);
            var aExchangeRateRate = aExchangeRateJson["rate"];
            var aValue = aExchangeRateRate.Value<double>();
            var aTimeJson = aExchangeRateJson["time"];
            var aDateTime = aTimeJson.Value<DateTime>();
            var aAmount = new CPrice((decimal)aValue);
            var aAmountWithCurrency = new CPriceWithCurrency(aAmount, aToCurrency);
            var aExchangeRate = new CExchangeRate(aDateTime, aFromCurrency, aAmountWithCurrency);
            return aExchangeRate;
        }
        
        CExchangeRatePeriod[] ICurrencyInfoProvider.GetExchangeRatePeriods(CExchangeRatePeriodQuery aQuery)
        {
            var aRequest = new CCaRequest(this.Trader.SettingsVm);
            var aInvertAndResponse = aRequest.GetHistoricalData(aQuery);
            var aInvert = aInvertAndResponse.Item1;
            var aPeriodsString = aInvertAndResponse.Item2;
            var aPeriodsJsons = JArray.Parse(aPeriodsString);

            var aPeriods = aPeriodsJsons.Select(aPeriodJson =>
            {
                var aBuyCurrency = aQuery.BuyCurrency;
                var aSellCurrency = aQuery.SellCurrency;
                var aTimePeriodStart = aPeriodJson.Value<DateTime>("time_period_start");
                var aTimePeriodEnd = aPeriodJson.Value<DateTime>("time_period_end");
                var aPriceOpenDecimal = aPeriodJson.Value<decimal>("price_open");
                var aPriceHighDecimal = aPeriodJson.Value<decimal>("price_high");
                var aPriceLowDecimal = aPeriodJson.Value<decimal>("price_low");
                var aPriceCloseDecimal = aPeriodJson.Value<decimal>("price_close");
                var aPriceOpen = new CPrice(aPriceOpenDecimal).Invert(aInvert);
                var aPriceHigh = new CPrice(aPriceHighDecimal).Invert(aInvert);
                var aPriceLow = new CPrice(aPriceLowDecimal).Invert(aInvert);
                var aPriceClose = new CPrice(aPriceCloseDecimal).Invert(aInvert);
                var aPeriod = new CExchangeRatePeriod(aSellCurrency, aBuyCurrency, aTimePeriodStart, aTimePeriodEnd, aPriceOpen, aPriceClose, aPriceLow, aPriceHigh);
                return aPeriod;
            }).ToArray();
            return aPeriods;
        }
        int ICurrencyInfoProvider.RequestsPerDay => 48;

    }
}
