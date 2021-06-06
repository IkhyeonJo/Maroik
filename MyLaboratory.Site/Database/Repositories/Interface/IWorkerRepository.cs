using MyLaboratory.Site.Database.Tables;
using System.Collections.Generic;

namespace MyLaboratory.Site.Database.Repositories.Interface
{
    public interface IWorkerRepository
    {
        IEnumerable<Worker> GetWorkers { get; }
    }
}