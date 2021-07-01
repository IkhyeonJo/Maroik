using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Contracts
{
    public interface IWorkerRepository
    {
        IEnumerable<Worker> GetWorkers { get; }
    }
}