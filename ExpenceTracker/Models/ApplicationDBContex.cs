using Microsoft.EntityFrameworkCore;

namespace ExpenceTracker.Models
{
    public class ApplicationDBContex:DbContext
    {
        public ApplicationDBContex(DbContextOptions options):base(options)
        {
            
        }
        public DbSet<Transaction>Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
    }
}
