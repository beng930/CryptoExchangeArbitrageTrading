using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Arbitrage_Trading
{
    public class LoggerService : ILogger
    {
        private CsvConfiguration configurations = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        };
        private List<Action> logs = new List<Action>();

        public void LogChosenPath(List<int> path, decimal finalAmount, int pathUniqueLength)
        {
            logs.Add(() =>
            {
                using (var fileStream = File.Open("", FileMode.Append))
                using (var streamWriter = new StreamWriter(fileStream))
                using (var csv = new CsvWriter(streamWriter, configurations))
                {
                    csv.WriteField(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                    csv.WriteField("Chosen path");
                    path.ForEach(item => csv.WriteField(ArbitrageConstants.nodeToSymbol[item % pathUniqueLength]));
                    csv.WriteField("Path final amount" + finalAmount);
                    csv.NextRecord();
                };
            });
        }

        public void LogMarketOrder(TradeOrderResponse tradeOrderResponse)
        {
            logs.Add(() =>
            {
                using (var fileStream = File.Open("", FileMode.Append))
                using (var streamWriter = new StreamWriter(fileStream))
                using (var csv = new CsvWriter(streamWriter, configurations))
                {
                    var date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    csv.WriteField(date);
                    csv.WriteField("Post order");
                    csv.WriteField(tradeOrderResponse.symbol);
                    csv.WriteField(tradeOrderResponse.side);
                    csv.WriteField(tradeOrderResponse.executedQty);
                    csv.WriteField(tradeOrderResponse.cummulativeQuoteQty);
                    csv.NextRecord();
                    csv.WriteField(date);
                    if (tradeOrderResponse.fills.Length > 0)
                    {
                        for (int i = 0; i < tradeOrderResponse.fills.Length; i++)
                        {
                            csv.WriteField(date);
                            csv.WriteField("fill " + i);
                            csv.WriteField(tradeOrderResponse.fills[i].price);
                            csv.WriteField(tradeOrderResponse.fills[i].qty);
                            csv.WriteField(tradeOrderResponse.fills[i].commission);
                            csv.WriteField(tradeOrderResponse.fills[i].commissionAsset);
                            csv.NextRecord();
                        }
                    }
                    else
                    {
                        csv.WriteField("Fills is empty, transaction was unsuccessful");
                        csv.NextRecord();
                    }
                }
            });
        }

        public void LogPreMarketOrder(string symbol, string side, string amount, bool retry = false)
        {
            logs.Add(() =>
            {
                using (var fileStream = File.Open("", FileMode.Append))
                using (var streamWriter = new StreamWriter(fileStream))
                using (var csv = new CsvWriter(streamWriter, configurations))
                {
                    var date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    csv.WriteField(date);
                    csv.WriteField("Pre order" + (retry ? "retry" : ""));
                    csv.WriteField(symbol);
                    csv.WriteField(side);
                    csv.WriteField(amount);
                    csv.NextRecord();
                }
            });
        }

        public void LogError(Exception ex)
        {
            logs.Add(() =>
            {
                using (var fileStream = File.Open("", FileMode.Append))
                using (var streamWriter = new StreamWriter(fileStream))
                using (var csv = new CsvWriter(streamWriter, configurations))
                {
                    var date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    csv.WriteField(date);
                    csv.WriteField(ex.Message);
                    csv.NextRecord();
                }
            });

            DumpLogs();
        }

        public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {}

        public void DumpLogs()
        {
            foreach (var log in logs)
            {
                if (log != null)
                {
                    log();
                }
            }

            logs.Clear();
        }
    }
}
