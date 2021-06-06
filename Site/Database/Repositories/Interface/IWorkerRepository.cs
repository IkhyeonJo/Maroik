using Site.Database.Tables;
using System.Collections.Generic;

namespace Site.Database.Repositories.Interface
{
    public interface IWorkerRepository
    {
        IEnumerable<Worker> GetWorkers { get; }
    }
}