using Microsoft.EntityFrameworkCore;
using AllusMVC.Models;

namespace AllusMVC.DataAccessLayer
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
    }
}
