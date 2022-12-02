namespace Arbitrage_Trading
{
    using Arbitrage_Trading.BusinessLogic;
    using Arbitrage_Trading.DirectAccessLayer;
    using Arbitrage_Trading.Logger;
    using Newtonsoft.Json;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        private static readonly LoggerService loggerService = new LoggerService();

        public static async Task Main(string[] args)
        {
            try
            {
                var arbitrageDal = new ArbitrageDal();
                var exchangeInfo = JsonConvert.DeserializeObject<ExchangeInformation>(
                    await arbitrageDal.ExchangeInformation(System.Text.Json.JsonSerializer.Serialize(ArbitrageConstants.tickers)));
                var arbitrageBL = new ArbitrageBusinessLogic(arbitrageDal, exchangeInfo, loggerService);

                var webSocket = arbitrageDal.CreateSpotWebSocket();
                var cancellation = new CancellationToken();
                webSocket.OnMessageReceived((data) =>
                {
                    var tickerUpdate = DeserializeUpdateData(data);
                    arbitrageBL.UpdateExchangeInformation(tickerUpdate);
                    Task.Run(() => arbitrageBL.RunBusinessLogic());
                    return Task.CompletedTask;
                }, cancellation);
                await webSocket.ConnectAsync(cancellation);

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                loggerService.LogError(ex);
            }
            finally
            {
                loggerService.DumpLogs();
            }
        }

        public static BookTickerUpdateInfo DeserializeUpdateData(string msg)
        {
            try
            {
                return JsonConvert.DeserializeObject<BookTickerUpdateInfo>(msg);
            }
            catch (Exception ex)
            {
                loggerService.LogError(ex);
                return null;
            }
        }
    }
}
