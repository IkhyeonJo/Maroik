using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Database.Repositories.Interface
{
    public interface IWorkerRepository
    {
        IEnumerable<Worker> GetWorkers { get; }
    }
}