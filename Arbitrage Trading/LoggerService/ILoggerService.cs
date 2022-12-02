using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Arbitrage_Trading.Logger
{
    public interface ILoggerService : ILogger
    {
        public void LogChosenPath(List<int> path, decimal finalAmount, int pathUniqueLength);

        public void LogMarketOrder(TradeOrderResponse tradeOrderResponse);

        public void LogPreMarketOrder(string symbol, string side, string amount, bool retry = false);

        public void LogError(Exception ex);

        public void DumpLogs();

    }
}
