using Microsoft.EntityFrameworkCore;

namespace MVCandKAFKA3;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    { }

    public DbSet<Product> Products { get; set; }
}
