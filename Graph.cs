namespace Arbitrage_Trading
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class Graph
    {
        protected internal int numberOfVertices;
        protected internal ConcurrentDictionary<int, List<int>> adjacencyList;

        public Graph(int numberOfVertices)
        {
            this.numberOfVertices = numberOfVertices;
            initAdjacencyList();
        }

        private void initAdjacencyList()
        {
            adjacencyList = new ConcurrentDictionary<int, List<int>>();

            for (int i = 0; i < numberOfVertices; i++)
            {
                adjacencyList[i] = new List<int>();
            }
        }

        public void addEdge(int from, int to)
        {
            adjacencyList[from].Add(to);
        }
    }
}
