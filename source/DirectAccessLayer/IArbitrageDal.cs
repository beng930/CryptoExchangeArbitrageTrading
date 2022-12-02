using Binance.Spot;
using Binance.Spot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Arbitrage_Trading.DirectAccessLayer
{
    public interface IArbitrageDal
    {
        public Task<string> TickerPrices(string symbols);

        public Task<string> ExchangeInformation(string symbols);

        public Task<string> NewOrder(string symbol, Side side, decimal amount, bool isQuote = false);

        public MarketDataWebSocket CreateSpotWebSocket();

        public string tickersSockets();
    }
}
