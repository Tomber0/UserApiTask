using Microsoft.EntityFrameworkCore;

namespace UserApiTask.Models
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(new Role() { Id = 1, Name = "" });
            modelBuilder.Entity<Role>().HasData(new Role() { Id = 2, Name = "" });
            modelBuilder.Entity<Role>().HasData(new Role() { Id = 3, Name = "" });
            modelBuilder.Entity<Role>().HasData(new Role() { Id = 4, Name = "" });
        }
    }
}
