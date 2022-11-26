# CryptoExchangeArbitrageTrading

This source code is built to act as a bot for leveraging arbitrage opportunities in crypto exchanges, specifically Binance.
It can be used with any exchange, given changes to the API calls and data stream.
The general system design is described in this flow chart:
![image](https://user-images.githubusercontent.com/39993978/204098237-df2ae003-04e2-493b-882a-6cb6462baff8.png)

Complexity - the algorithm for finding all paths from source to destination is O(N!), which is extremely demanding.
For that matter, the graph was designed to hold only 9 different coins (making the graph with 10 nodes). 
In addition, we are not trying to find the best route, but one that gain revenue above a certain constant, so if there is indeed an opportunity for arbitrage, we won’t have to go through all 10! options (in most cases) before finding it. 

Another big factor for speeding up the time is limiting the path length. 
This is extremely important for executing the transactions as well, since API calls are very slow (and we have to execute them sequentially). 
As a rule of thumb, the quicker we finish, the higher the chances the arbitrage opportunity is still available.

I’ve decided to work with Binance, mostly since I already own some assets there, so it would be easy to move things around. 
Their API is also pretty well documented and used by the community.
One thing that I really did not like, is that there is no option to send messages through their web socket (calling it WebSocketDataStream, so basically DataStream), which heavily cost us in executing the transactions found on the revenue path. 

I’ve implemented a Graph and GraphUtils concurrent classes to support the algorithm and updating exchange rates through Binance data stream.

In summary, our program awaits updates from Binance data stream and updates the graph when each update arrives. 
Concurrently, we are looking for revenue paths in the graph. 
The search is executed only on one thread (while many others update the graph), for two reasons:
If there is an arbitrage opportunity, I wish to take advantage of it as quickly as possible. contact switching (other than for updates) and running multiple searches at once don’t serve us well in that sense. 
In addition, executing the transactions when a revenue path was found, should be quick as possible.
Other threads update the graph during the search so we’re mostly not missing any new information while we search (reader - writer lock through C# concurrent collections).
