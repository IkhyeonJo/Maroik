using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Database.Config;
using MyLaboratory.Site.Database.Repositories.Interface;
using MyLaboratory.Site.Database.Tables;

namespace MyLaboratory.Site.Database.Repositories.Class
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