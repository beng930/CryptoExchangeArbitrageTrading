using Binance.Common;
using Binance.Spot;
using Binance.Spot.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace Arbitrage_Trading.DirectAccessLayer
{
    public class ArbitrageDal : IArbitrageDal
    {
        private const string apiKey = "";
        private const string apiSecret = "";

        public ArbitrageDal()
        {}

        public Task<string> TickerPrices(string symbols)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole();
            });
            ILogger logger = loggerFactory.CreateLogger<ArbitrageDal>();

            HttpMessageHandler loggingHandler = new BinanceLoggingHandler(logger: logger);
            HttpClient httpClient = new HttpClient(handler: loggingHandler);

            var market = new Market(httpClient);

            return market.SymbolOrderBookTicker(symbols: symbols);
        }

        public Task<string> ExchangeInformation(string symbols)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole();
            });
            ILogger logger = loggerFactory.CreateLogger<ArbitrageDal>();

            HttpMessageHandler loggingHandler = new BinanceLoggingHandler(logger: logger);
            HttpClient httpClient = new HttpClient(handler: loggingHandler);

            var market = new Market(httpClient);

            return market.ExchangeInformation(symbols: symbols);
        }

        public Task<string> NewOrder(string symbol, Side side, decimal amount, bool isQuote = false)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole();
            });
            ILogger logger = loggerFactory.CreateLogger<ArbitrageDal>();

            HttpMessageHandler loggingHandler = new BinanceLoggingHandler(logger: logger);
            HttpClient httpClient = new HttpClient(handler: loggingHandler);

            var spotAccountTrade = new SpotAccountTrade(httpClient, apiKey: apiKey, apiSecret: apiSecret);

            if (isQuote)
            {
                return spotAccountTrade.NewOrder(symbol, side, OrderType.MARKET, quoteOrderQty: amount);
            }

            return spotAccountTrade.NewOrder(symbol, side, OrderType.MARKET, quantity: amount);
        }

        public MarketDataWebSocket CreateSpotWebSocket()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole();
            });
            ILogger logger = loggerFactory.CreateLogger<ArbitrageDal>();
            HttpMessageHandler loggingHandler = new BinanceLoggingHandler(logger: logger);
            HttpClient httpClient = new HttpClient(handler: loggingHandler);

            return new MarketDataWebSocket(tickersSockets());
        }

        public string tickersSockets()
        {
            string tickerSocketSuffix = "@bookTicker";
            string tickersSocketsString = "";
            foreach (var ticker in ArbitrageConstants.tickers)
            {
                tickersSocketsString += ticker.ToLower() + tickerSocketSuffix + "/";
            }

            return tickersSocketsString.Substring(0, tickersSocketsString.Length - 1);
        }
    }
}
