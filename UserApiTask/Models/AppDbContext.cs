using Microsoft.EntityFrameworkCore;

namespace UserApiTask.Models
{
    public class AppDbContext:DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Role>().HasData(new Role() { Id = 1, Name = "User" });
            modelBuilder.Entity<Role>().HasData(new Role() { Id = 2, Name = "Admin" });
            modelBuilder.Entity<Role>().HasData(new Role() { Id = 3, Name = "Support" });
            modelBuilder.Entity<Role>().HasData(new Role() { Id = 4, Name = "SuperAdmin" });
        }
    }
}
