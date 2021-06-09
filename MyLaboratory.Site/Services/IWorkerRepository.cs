using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Services
{
    public interface IWorkerRepository
    {
        IEnumerable<Worker> GetWorkers { get; }
    }
}