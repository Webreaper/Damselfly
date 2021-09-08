using System;
using System.Threading.Tasks;

namespace Damselfly.Core.Interfaces
{
    public interface ITransactionThrottle
    {
        Task<T> Call<T>(string desc, Task<T> method);
        Task Run(string desc, Task method);
        void ProcessNewTransactions();
        bool Disabled { get;  }
        void SetLimits(int maxTransPerMin, int maxTransPerMonth);
    }
}
