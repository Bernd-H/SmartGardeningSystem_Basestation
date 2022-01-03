using System.Threading.Tasks;

namespace GardeningSystem.Common.Specifications.Managers {
    public interface IAesKeyExchangeManager {
        Task StartListener();

        void Stop();
    }
}
