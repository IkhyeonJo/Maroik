using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Site.Database.Config;
using Site.Database.Repositories.Interface;
using Site.Database.Tables;

namespace Site.Database.Repositories.Class
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