using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TIKR.Infrastructure.Data;

public class TikrDbContextFactory : IDesignTimeDbContextFactory<TikrDbContext>
{
    public TikrDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TikrDbContext>();
        optionsBuilder.UseSqlite("Data Source=tikr-design.db");
        return new TikrDbContext(optionsBuilder.Options);
    }
}
