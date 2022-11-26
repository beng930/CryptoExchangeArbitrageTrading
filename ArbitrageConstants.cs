using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arbitrage_Trading
{
    public static class ArbitrageConstants
    {
        public static string Bitcoin = "BTC";
        public static string USDT = "USDT";
        public static string Etherium = "ETH";
        public static string Solana = "SOL";
        public static string BUSD = "BUSD";
        public static string BNB = "BNB";
        public static string Cardano = "ADA";
        public static string Algo = "ALGO";
        public static string Xrp = "XRP";

        public static List<string> supportedCoins = new List<string> { Bitcoin, USDT, Etherium, Solana, BUSD, BNB, Cardano, Algo, Xrp };

        public static string[] tickers = new string[]{
            "BTCUSDT","BTCBUSD","ETHBTC","ETHUSDT","ETHBUSD","SOLBTC","SOLUSDT","SOLETH","SOLBUSD",
            "SOLBNB","BUSDUSDT","BNBBTC","BNBETH","BNBUSDT","BNBBUSD","ADAUSDT","ADABUSD","ADABNB",
            "ADABTC","ADAETH","ALGOUSDT","ALGOBTC","ALGOETH","ALGOBUSD","ALGOBNB","XRPUSDT","XRPBUSD",
            "XRPBNB","XRPBTC","XRPETH"
        };

        public static Dictionary<string, int> symbolToNode = new Dictionary<string, int>()
        {
            { USDT, 0},
            { Bitcoin, 1},
            { Etherium, 2},
            { Solana, 3},
            { BUSD, 4},
            { BNB, 5},
            { Cardano, 6},
            { Algo, 7},
            { Xrp, 8},
        };

        public static Dictionary<int, string> nodeToSymbol = new Dictionary<int, string>()
        {
            { 0, USDT},
            { 1, Bitcoin },
            { 2, Etherium },
            { 3, Solana },
            { 4, BUSD },
            { 5, BNB },
            { 6, Cardano },
            { 7, Algo },
            { 8, Xrp },
        };

        public static decimal RoundToNearest(decimal passedNumber, decimal roundTo, bool isDown)
        {
            if (roundTo == 0)
            {
                return passedNumber;
            }
            else
            {
                return isDown ? Math.Floor(passedNumber / roundTo) * roundTo : Math.Ceiling(passedNumber / roundTo) * roundTo;
            }
        }
    }

    public class BookTickerInfo
    {
        public string bidPrice;
        public string askPrice;
        public string symbol;
    }

    public class BookTickerUpdateInfo
    {
        [JsonProperty("u")]
        public string u;
        [JsonProperty("s")]
        public string symbol;
        [JsonProperty("b")]
        public string bidPrice;
        [JsonProperty("B")]
        public string B;
        [JsonProperty("a")]
        public string askPrice;
        [JsonProperty("A")]
        public string A;
    }

    public class TradeOrderResponse
    {
        public string symbol;
        public string executedQty;
        public string cummulativeQuoteQty;
        public string side;
        public Fills[] fills;
    }

    public class Fills
    {
        public string price { get; set; }
        public string qty { get; set; }
        public string commission { get; set; }
        public string commissionAsset { get; set; }
        public double tradeId { get; set; }
    }

    public class ExchangeInformation
    {
        public Filter[] exchangeFilters;
        public Symbol[] symbols;
    }
    public class Symbol
    {
        public string symbol;
        public Filter[] filters;
    }

    public class Filter
    {
        public string filterType;
        public string stepSize;
    }
}
