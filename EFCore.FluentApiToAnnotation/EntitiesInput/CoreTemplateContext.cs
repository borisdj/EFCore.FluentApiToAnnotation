using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CoreTemplate.Entities2
{
    public partial class CoreTemplateContext : DbContext
    {
        public virtual DbSet<Company> Company { get; set; }
        public virtual DbSet<Group> Group { get; set; }
        public virtual DbSet<Item> Item { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseSqlServer(@"Server=localhost;Database=CoreTemplate;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(e => e.CompanyId).ValueGeneratedNever();

                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.Property(e => e.GroupId).ValueGeneratedNever();

                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("Item", "fin");

                entity.HasIndex(e => e.CompanyId)
                    .HasName("IX_Item_CompanyId");

                entity.HasIndex(e => e.Description)
                    .HasName("IX_Item_Description")
                    .IsUnique();

                entity.HasIndex(e => e.GroupId)
                    .HasName("IX_Item_GroupId");

                entity.Property(e => e.ItemId).ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Price).HasColumnType("decimal");

                entity.Property(e => e.PriceExtended).HasColumnType("decimal(20,4)");

                entity.Property(e => e.TimeExpire).HasColumnType("datetime");

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.Item)
                    .HasForeignKey(d => d.CompanyId);

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.Item)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}