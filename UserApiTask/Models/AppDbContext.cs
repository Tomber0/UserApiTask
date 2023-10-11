using Microsoft.EntityFrameworkCore;

namespace UserApiTask.Models
{
    public class AppDbContext:DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //base.OnModelCreating(modelBuilder);
            var role1 = new Role()
            {
                Id = 1,
                Name = "User"
            };
            var role2 = new Role()
            {
                Id = 2,

                Name = "Admin"
            };
            modelBuilder.Entity<Role>().HasData(role1,role2, new Role() { Id = 3, Name = "Support" }, new Role() { Id = 4, Name = "SuperAdmin" });

            modelBuilder.Entity<User>().HasData(new User() { Id = 1, Name = "User", Age = 22, Email = "user@mail.com"});
            modelBuilder.Entity<User>().HasData(new User() { Id = 2, Name = "User2", Age = 22, Email = "user2@mail.com"});
            modelBuilder.Entity<User>().HasData(new User() { Id = 3, Name = "User3", Age = 22, Email = "user3@mail.com" });

        }
    }
}
