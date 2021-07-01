using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Models;
using MyLaboratory.Site.Contracts;

namespace MyLaboratory.Site.Services
{
    public class WorkerRepository : IWorkerRepository
    {
        private readonly ApplicationDbContext context;

        public WorkerRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IEnumerable<Worker> GetWorkers
        {
            get
            {
                var list = context.Workers.FromSqlRaw("SELECT * FROM Worker").ToList();
                return list;
            }
            set
            {

            }
        }
    }
}