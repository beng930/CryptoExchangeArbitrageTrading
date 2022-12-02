using System.Threading.Tasks;

namespace Arbitrage_Trading.BusinessLogic
{
    public interface IArbitrageBusinessLogic
    {
        public Task RunBusinessLogic();
        public void UpdateExchangeInformation(BookTickerUpdateInfo bookTickerInfo);
    }
}
