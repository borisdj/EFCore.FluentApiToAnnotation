using EfCore.Shaman;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CoreTemplate.Entities
{
    public partial class CoreTemplateContext : DbContext
    {
        public CoreTemplateContext(DbContextOptions options) : base(options) { }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Item> Items { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            this.FixOnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Item>().HasOne(p => p.Group).WithMany().OnDelete(DeleteBehavior.Restrict);
        }
    }
}
