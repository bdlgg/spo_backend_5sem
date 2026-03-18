using Microsoft.EntityFrameworkCore;
using backend_spo.Entities;

namespace backend_spo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Car> Cars { get; set; }

        public DbSet<Client> Clients { get; set; }
    }
}