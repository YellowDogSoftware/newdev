using Microsoft.EntityFrameworkCore;
using YellowDogSoftware.NewDev.Models;

namespace YellowDogSoftware.NewDev.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options)
        : base(options)
    {
        //
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);

            e.Property(u => u.Id)
                .ValueGeneratedNever();

            e.Property(u => u.CreatedAt)
                .HasConversion<long>();
        });
    }
}