using Binance.Common;
using Binance.Spot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbitrage_Trading.GraphLogic;
using Arbitrage_Trading.DirectAccessLayer;
using Arbitrage_Trading.Logger;

namespace Arbitrage_Trading.BusinessLogic
{
    public class ArbitrageBusinessLogic : IArbitrageBusinessLogic
    {
        private readonly ILoggerService loggerService;
        private readonly IArbitrageDal arbitrageDal;
        private readonly ExchangeInformation exchangeInformation;
        public Graph graph;
        public GraphUtils graphUtils;

        private const decimal initialUSDTAmount = 18;
        private const decimal AtomicityFactorConst = 0.00001M;
        private const decimal ExchangeFeeConst = 0.001M;
        private static int CoinsGraphEntries = ArbitrageConstants.supportedCoins.Count + 1;
        private static int NumberOfSupportedCoins = ArbitrageConstants.supportedCoins.Count;
        private static int CriticalCodeLock = 0;

        public ArbitrageBusinessLogic(IArbitrageDal arbitrageDal, ExchangeInformation exchangeInformation, ILoggerService loggerService)
        {
            this.arbitrageDal = arbitrageDal;
            this.exchangeInformation = exchangeInformation;
            this.loggerService = loggerService;

            this.graph = new Graph(CoinsGraphEntries);
            this.graphUtils = new GraphUtils(CoinsGraphEntries, graph);
            BuildExchangeGraph(graph);
        }

        public async Task RunBusinessLogic()
        {
            if (Interlocked.CompareExchange(ref CriticalCodeLock, 1, 0) == 1)
            {
                return;
            }

            try
            {
                var (list, currencyAmount) = graphUtils.FindPathWithRevenue(0, NumberOfSupportedCoins, initialUSDTAmount, initialUSDTAmount * (1 + ExchangeFeeConst));

                if (currencyAmount > 0 && list.Count > 0 && list.Count() <= 6)
                {
                    loggerService.LogChosenPath(list, currencyAmount, list.Count);
                    List<decimal> MarketOrdersAmounts = new List<decimal> { initialUSDTAmount };
                    Console.Write(list[0] + " --> ");

                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        Console.Write(list[i + 1] + " --> ");
                        var tradeOrderResponse = await IssueMarketOrder(list[i], list[i + 1], MarketOrdersAmounts);
                        loggerService.LogMarketOrder(tradeOrderResponse);
                    }

                    Console.WriteLine("\n" + currencyAmount.ToString());
                }
            }
            catch (Exception ex)
            {
                loggerService.LogError(ex);
                return;
            }
            finally
            {
                loggerService.DumpLogs();
                CriticalCodeLock = 0;
            }
        }

        public void UpdateExchangeInformation(BookTickerUpdateInfo bookTickerInfo)
        {
            foreach (var firstCoin in ArbitrageConstants.supportedCoins)
            {
                foreach (var secondCoin in ArbitrageConstants.supportedCoins)
                {
                    if (bookTickerInfo?.symbol == firstCoin + secondCoin)
                    {
                        UpdateTickerToExchangeGraph(graph, bookTickerInfo, firstCoin, secondCoin);
                    }
                }
            }
        }

        private void BuildExchangeGraph(Graph graph)
        {
            try
            {
                var tickerInfoTask = Task.Run(() => arbitrageDal.TickerPrices(System.Text.Json.JsonSerializer.Serialize(ArbitrageConstants.tickers)));
                tickerInfoTask.Wait();
                var tickersList = JsonConvert.DeserializeObject<List<BookTickerInfo>>(tickerInfoTask.Result);

                // Can make more efficient going over half of the array and doing both pairs in one iteration
                foreach (var firstCoin in ArbitrageConstants.supportedCoins)
                {
                    foreach (var secondCoin in ArbitrageConstants.supportedCoins)
                    {// Can create a dictionary instead of iterating (this happens only once, so no biggy)
                        var ticker = tickersList.Find(element => element.symbol == $"{firstCoin}{secondCoin}");
                        if (ticker != default)
                        {
                            AddTickerToExchangeGraph(graph, ticker, firstCoin, secondCoin);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                loggerService.LogError(ex);
            }
        }

        private async Task<TradeOrderResponse> IssueMarketOrder(int fromCoinId, int toCoinId, List<decimal> MarketOrdersAmount)
        {
            var fromCoin = ArbitrageConstants.nodeToSymbol[fromCoinId % NumberOfSupportedCoins];
            var toCoin = ArbitrageConstants.nodeToSymbol[toCoinId % NumberOfSupportedCoins];
            TradeOrderResponse response;
            if (ArbitrageConstants.tickers.Contains(fromCoin + toCoin))
            {
                response = await TryHighestSell(fromCoin + toCoin, MarketOrdersAmount);
            }
            else
            {
                //Buy transactions don't have issues exceeding account funds (will sell the max possible)
                loggerService.LogPreMarketOrder(toCoin + fromCoin, Side.BUY, MarketOrdersAmount.Last().ToString());
                response = JsonConvert.DeserializeObject<TradeOrderResponse> 
                    (await arbitrageDal.NewOrder(toCoin + fromCoin, Side.BUY, MarketOrdersAmount.Last(), isQuote: true));
                MarketOrdersAmount.Add(CalculateAmountAfterTransaction(response, isCummulative: false));
            }

            return response;
        }

        private async Task<TradeOrderResponse> TryHighestSell(string symbol, List<decimal> MarketOrdersAmount)
        {
            try
            {
                var roundUpByStepSizeAmount = RoundByStepSize(symbol, MarketOrdersAmount.Last(), isDown: false);
                loggerService.LogPreMarketOrder(symbol, Side.SELL, roundUpByStepSizeAmount.ToString());
                var response = JsonConvert.DeserializeObject<TradeOrderResponse>
                    (await arbitrageDal.NewOrder(symbol, Side.SELL, roundUpByStepSizeAmount));
                MarketOrdersAmount.Add(CalculateAmountAfterTransaction(response, isCummulative: true));
                return response;
            }
            catch (BinanceClientException ex) when (ex.Message.Contains(""))//check what is sent when there isn't enough funds
            {
                var roundDownByStepSizeAmount = RoundByStepSize(symbol, MarketOrdersAmount.Last(), isDown: true);
                loggerService.LogPreMarketOrder(symbol, Side.SELL, roundDownByStepSizeAmount.ToString(), retry: true);
                var response = JsonConvert.DeserializeObject<TradeOrderResponse>
                    (await arbitrageDal.NewOrder(symbol, Side.SELL, roundDownByStepSizeAmount));
                MarketOrdersAmount.Add(CalculateAmountAfterTransaction(response, isCummulative: true));
                return response;
            }
        }

        private decimal CalculateAmountAfterTransaction(TradeOrderResponse tradeOrderResponse, bool isCummulative)
        {
            var totalAmount = decimal.Parse(isCummulative ? 
                tradeOrderResponse.cummulativeQuoteQty: tradeOrderResponse.executedQty);
            foreach (var singleTransactionCommision in tradeOrderResponse.fills)
            {
                totalAmount -= decimal.Parse(singleTransactionCommision.commission);
            }

            return totalAmount;
        }

        private decimal RoundByStepSize(string symbol, decimal totalAmount, bool isDown)
        {
            var stepSize = decimal.Parse(
                exchangeInformation.symbols
                .First(symbolInfo => symbolInfo.symbol == symbol).filters
                .First(filter => filter.filterType == "LOT_SIZE").stepSize
            );

            return ArbitrageConstants.RoundToNearest(totalAmount, stepSize, isDown);
        }

        private decimal[][] CreateExchangeRateArray()
        {
            var exchangeRates = new decimal[CoinsGraphEntries][];
            for (int i = 0; i < CoinsGraphEntries; i++)
            {
                exchangeRates[i] = new decimal[CoinsGraphEntries];
            }

            return exchangeRates;
        }

        private void AddTickerToExchangeGraph(Graph graph, BookTickerInfo ticker, string firstCoin, string secondCoin, bool isUpdate = true)
        {
            var firstCoinId = ArbitrageConstants.symbolToNode[firstCoin];
            var secondCoinId = ArbitrageConstants.symbolToNode[secondCoin];

            if (firstCoinId == 0)
            {
                AddExchangeRate(graph, firstCoinId, secondCoinId, ticker.askPrice);
                AddExchangeRate(graph, secondCoinId, NumberOfSupportedCoins, ConvertBidPrice(ticker.bidPrice));
                return;
            }
            if (secondCoinId == 0)
            {
                AddExchangeRate(graph, firstCoinId, NumberOfSupportedCoins, ticker.askPrice);
                AddExchangeRate(graph, secondCoinId, firstCoinId, ConvertBidPrice(ticker.bidPrice));
                return;
            }

            AddExchangeRate(graph, firstCoinId, secondCoinId, ticker.askPrice);
            AddExchangeRate(graph, secondCoinId, firstCoinId, ConvertBidPrice(ticker.bidPrice));
        }

        private void UpdateTickerToExchangeGraph(Graph graph, BookTickerUpdateInfo ticker, string firstCoin, string secondCoin)
        {
            var firstCoinId = ArbitrageConstants.symbolToNode[firstCoin];
            var secondCoinId = ArbitrageConstants.symbolToNode[secondCoin];

            if (firstCoinId == 0)
            {
                UpdateExchangeRate(graph, firstCoinId, secondCoinId, ticker.askPrice);
                UpdateExchangeRate(graph, secondCoinId, NumberOfSupportedCoins, ConvertBidPrice(ticker.bidPrice));
                return;
            }
            if (secondCoinId == 0)
            {
                UpdateExchangeRate(graph, firstCoinId, NumberOfSupportedCoins, ticker.askPrice);
                UpdateExchangeRate(graph, secondCoinId, firstCoinId, ConvertBidPrice(ticker.bidPrice));
                return;
            }

            UpdateExchangeRate(graph, firstCoinId, secondCoinId, ticker.askPrice);
            UpdateExchangeRate(graph, secondCoinId, firstCoinId, ConvertBidPrice(ticker.bidPrice));
        }

        private string ConvertBidPrice(string bidPrice)
        {
            return (1 / decimal.Parse(bidPrice)).ToString();
        }

        private void AddExchangeRate(Graph graph, int firstCoinId, int secondCoinId, string price)
        {
            if (string.IsNullOrEmpty(price) == false)
            {
                UpdateExchangeRate(graph, firstCoinId, secondCoinId, price);
                graph.addEdge(firstCoinId, secondCoinId);
            }
        }

        private void UpdateExchangeRate(Graph graph, int firstCoinId, int secondCoinId, string price)
        {
            if (string.IsNullOrEmpty(price) == false)
            {
                var priceConverted = decimal.Parse(price);
                graphUtils.UpdateExchangeArray(firstCoinId, secondCoinId, priceConverted - (priceConverted * (AtomicityFactorConst + ExchangeFeeConst)));
            }
        }

        private void PrintExchangeArray(decimal[][] exchangeRates)
        {
            for (int i = 0; i < NumberOfSupportedCoins; i++)
            {
                for (int j = 0; j < NumberOfSupportedCoins; j++)
                {
                    Console.Write(exchangeRates[i][j] + ",");
                }
                Console.WriteLine("\n");
            }
        }
    }
}
