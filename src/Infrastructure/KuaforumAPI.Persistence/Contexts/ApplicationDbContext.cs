using KuaforumAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KuaforumAPI.Persistence.Contexts
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<SalonOwnerApplication> SalonOwnerApplications { get; set; }
        public DbSet<Shop> Shops { get; set; }
        public DbSet<ShopImage> ShopImages { get; set; }
        public DbSet<ShopEmployee> ShopEmployees { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<ShopService> ShopServices { get; set; }
        public DbSet<ShopEmployeeService> ShopEmployeeServices { get; set; }
        public DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Shop Configuration
            builder.Entity<Shop>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(250);
                entity.Property(e => e.City).IsRequired().HasMaxLength(50);
                entity.Property(e => e.District).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                
                entity.HasOne(s => s.Owner)
                    .WithMany()
                    .HasForeignKey(s => s.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict); 
            });

            // ShopImage Configuration
            builder.Entity<ShopImage>(entity =>
            {
                entity.HasOne(si => si.Shop)
                    .WithMany(s => s.Images)
                    .HasForeignKey(si => si.ShopId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ShopEmployee Configuration
            builder.Entity<ShopEmployee>(entity =>
            {
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.StartDate).IsRequired();

                entity.HasOne(se => se.Shop)
                    .WithMany()
                    .HasForeignKey(se => se.ShopId)
                    .OnDelete(DeleteBehavior.Restrict); 

                entity.HasOne(se => se.User)
                    .WithMany()
                    .HasForeignKey(se => se.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Service Configuration
            builder.Entity<ServiceCategory>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(250);

                entity.HasOne(sc => sc.Shop)
                    .WithMany()
                    .HasForeignKey(sc => sc.ShopId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ShopService>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).IsRequired();
                entity.Property(e => e.Duration).IsRequired();

                entity.HasOne(ss => ss.Shop)
                    .WithMany()
                    .HasForeignKey(ss => ss.ShopId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ss => ss.Category)
                    .WithMany() // Assuming Category doesn't have a list of services navigation property yet
                    .HasForeignKey(ss => ss.CategoryId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ShopEmployeeService Configuration
            builder.Entity<ShopEmployeeService>(entity =>
            {
                entity.HasOne(ses => ses.ShopEmployee)
                    .WithMany()
                    .HasForeignKey(ses => ses.ShopEmployeeId)
                    .OnDelete(DeleteBehavior.Cascade); // Employee silinirse yetenekleri de silinsin.

                entity.HasOne(ses => ses.ShopService)
                    .WithMany()
                    .HasForeignKey(ses => ses.ShopServiceId)
                    .OnDelete(DeleteBehavior.Cascade); // Hizmet silinirse atamalar da silinsin.
                    // Cascade here is generally fine unless it causes cycles.
                    // Employee -> Shop
                    // Service -> Shop
                    // Link -> Employee AND Link -> Service
                    // Deleting Shop cascades to Employee and Service.
                    // Deleting Employee cascades to Link.
                    // Deleting Service cascades to Link.
                    // This creates multiple paths from Shop to Link (Shop->Employee->Link AND Shop->Service->Link).
                    // SQL Server will likely complain about "multiple cascade paths".
                    // So we must Restrict one of them.
                    // Let's Restrict Service deletion. If a service is assigned to employees, maybe don't delete it or handle manually?
                    // Actually, let's Restrict BOTH to be safe and consistent with previous fixes, or strictly follow the graph.
                    // Let's try NoAction/Restrict on Service side.
            });
            // Updating logic above:
            builder.Entity<ShopEmployeeService>(entity =>
            {
                entity.HasOne(ses => ses.ShopEmployee)
                    .WithMany()
                    .HasForeignKey(ses => ses.ShopEmployeeId)
                    .OnDelete(DeleteBehavior.Restrict); 

                entity.HasOne(ses => ses.ShopService)
                    .WithMany()
                    .HasForeignKey(ses => ses.ShopServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // EmployeeSchedule Configuration
            builder.Entity<EmployeeSchedule>(entity =>
            {
                entity.HasOne(es => es.ShopEmployee)
                    .WithMany()
                    .HasForeignKey(es => es.ShopEmployeeId)
                    .OnDelete(DeleteBehavior.Cascade); // Employee silinirse takvimi de silinsin.

                // Composite unique constraint might be good: One schedule per employee per day
                // builder.Entity<EmployeeSchedule>().HasIndex(es => new { es.ShopEmployeeId, es.DayOfWeek }).IsUnique(); 
                // But BaseEntity has Id, so it's fine. Application logic should ensure uniqueness.
            });

            // Appointment Configuration
            builder.Entity<Appointment>(entity =>
            {
                entity.HasOne(a => a.Shop)
                    .WithMany()
                    .HasForeignKey(a => a.ShopId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.ShopService)
                    .WithMany()
                    .HasForeignKey(a => a.ShopServiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.ShopEmployee)
                    .WithMany()
                    .HasForeignKey(a => a.ShopEmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // UserAddress Configuration
            builder.Entity<UserAddress>(entity =>
            {
                entity.Property(e => e.Title).IsRequired().HasMaxLength(50);
                entity.Property(e => e.City).IsRequired().HasMaxLength(50);
                entity.Property(e => e.District).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OpenAddress).IsRequired().HasMaxLength(200);

                entity.HasOne(ua => ua.User)
                    .WithMany(u => u.Addresses)
                    .HasForeignKey(ua => ua.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public DbSet<CoreExample> CoreExamples { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<KuaforumAPI.Domain.Common.BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
