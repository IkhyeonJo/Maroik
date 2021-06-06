using System.Collections.Generic;
namespace Site.Models.Home
{
    public interface IWorkerRepository
    {
        IEnumerable<Worker> GetWorkers { get; }
    }
}