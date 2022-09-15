using System.Threading.Tasks;

namespace Damselfly.Core.Interfaces;

public interface ITransactionThrottle
{
    bool Disabled { get; }
    Task<T> Call<T>(string desc, Task<T> method);
    Task Run(string desc, Task method);
    void ProcessNewTransactions();
    void SetLimits(int maxTransPerMin, int maxTransPerMonth);
}