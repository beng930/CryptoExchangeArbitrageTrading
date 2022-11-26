using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arbitrage_Trading
{
    public class GraphUtils
    {
        private Graph graph;
        private ConcurrentDictionary<int, ConcurrentDictionary<int, decimal>> exchangeRates;
        private List<int> chosenPath = new List<int>();
        private decimal calculatedPathCurrencyAmount;

        public GraphUtils(int numberOfVertices, Graph graph)
        {
            this.graph = graph;
            exchangeRates = new ConcurrentDictionary<int, ConcurrentDictionary<int, decimal>>();
            for (int i = 0; i < numberOfVertices; i++)
            {
                exchangeRates[i] = new ConcurrentDictionary<int, decimal>();
                for (int j = 0; j < numberOfVertices; j++)
                {
                    exchangeRates[i][j] = 0;
                }
            }
        }

        public void UpdateExchangeArray(int from, int to, decimal value)
        {
            exchangeRates[from][to] = value;
        }

        public void FillExchangeArray(decimal[][] exchangeRatesInput)
        {
            if (exchangeRates == null || exchangeRatesInput == null || exchangeRates.Count == 0)
            {
                return;
            }

            var numberOfRows = exchangeRates.Count > exchangeRatesInput.Length ? exchangeRatesInput.Length : exchangeRates.Count;
            var numberOfColumns = exchangeRates[0].Count > exchangeRatesInput[0].Length ? exchangeRatesInput[0].Length : exchangeRates[0].Count;
            for (int i = 0; i < numberOfRows; i++)
            {
                for (int j = 0; j < numberOfColumns; j++)
                {
                    exchangeRates[i][j] = exchangeRatesInput[i][j];
                }
            }
        }

        public (List<int>, decimal) FindPathWithRevenue(int start, int destination, decimal amountToTrade, decimal threshold)
        {
            bool[] isVisited = new bool[graph.numberOfVertices];
            List<int> pathList = new List<int>();
            pathList.Add(start);

            FindPathWithRevenueUtility(start, destination, isVisited, pathList, amountToTrade, threshold);
            var pathWithRevenue = new List<int>(chosenPath);
            var calculatedRevenue = calculatedPathCurrencyAmount;
            chosenPath.Clear();
            calculatedPathCurrencyAmount = 0;
            return (pathWithRevenue, calculatedRevenue);
        }

        private bool FindPathWithRevenueUtility(
            int from,
            int to,
            bool[] isVisited,
            List<int> localPathList,
            decimal currentAmountOfCurrency,
            decimal threshold)
        {
            if (from.Equals(to))
            {
                if (currentAmountOfCurrency > threshold)
                {
                    localPathList.ForEach(vertex => chosenPath.Add(vertex));
                    calculatedPathCurrencyAmount = currentAmountOfCurrency;
                    return true;
                }

                return false;
            }

            isVisited[from] = true;
            foreach (var index in graph.adjacencyList[from])
            {
                if (!isVisited[index])
                {
                    localPathList.Add(index);
                    // exchangeRates already take fees and highest bid into consideration
                    if (FindPathWithRevenueUtility(
                        index,
                        to,
                        isVisited,
                        localPathList,
                        currentAmountOfCurrency * exchangeRates[from][index], threshold))
                    {
                        return true; //fold the recursion
                    }

                    localPathList.Remove(index);
                }
            }

            // Mark the current node
            isVisited[from] = false;
            return false;
        }
    }
}
