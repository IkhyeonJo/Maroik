using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Site.Models.Home
{
    public class WorkerRepository : IWorkerRepository
    {
        private readonly AppDbContext context;

        public WorkerRepository(AppDbContext context)
        {
            this.context = context;
        }

        public IEnumerable<Worker> GetWorkers
        {
            get
            {
                var list = context.Worker.FromSqlRaw("SELECT * FROM Worker").ToList();
                return list;
            }
            set
            {

            }
        }
    }
}