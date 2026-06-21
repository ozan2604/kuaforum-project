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
        public DbSet<ShopImageTag> ShopImageTags { get; set; }
        public DbSet<UserFavoriteShop> UserFavoriteShops { get; set; }
        public DbSet<ShopEmployee> ShopEmployees { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<ShopService> ShopServices { get; set; }
        public DbSet<ShopEmployeeService> ShopEmployeeServices { get; set; }
        public DbSet<EmployeeSchedule> EmployeeSchedules { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }
        public DbSet<ShopCategoryAssignment> ShopCategoryAssignments { get; set; }
        public DbSet<SalonApplicationCategoryItem> SalonApplicationCategoryItems { get; set; }
        public DbSet<ShopClosureDate> ShopClosureDates { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<OtpCode> OtpCodes { get; set; }
        public DbSet<EmployeeLeaveDate> EmployeeLeaveDates { get; set; }
        public DbSet<ShopBlockedCustomer> ShopBlockedCustomers { get; set; }
        public DbSet<ShopVideo> ShopVideos { get; set; }
        public DbSet<MobileShopServiceArea> MobileShopServiceAreas { get; set; }
        public DbSet<MediaLike> MediaLikes { get; set; }

        private readonly KuaforumAPI.Application.Interfaces.Services.IDateTimeService _dateTimeService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, KuaforumAPI.Application.Interfaces.Services.IDateTimeService dateTimeService) : base(options)
        {
            _dateTimeService = dateTimeService;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Shop Configuration
            builder.Entity<Shop>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Address).HasMaxLength(250);
                entity.Property(e => e.City).HasMaxLength(50);
                entity.Property(e => e.District).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Code).HasMaxLength(10);

                entity.HasOne(s => s.Owner)
                    .WithMany()
                    .HasForeignKey(s => s.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(s => s.ServiceAreas)
                    .WithOne(a => a.Shop)
                    .HasForeignKey(a => a.ShopId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MobileShopServiceArea Configuration
            builder.Entity<MobileShopServiceArea>(entity =>
            {
                entity.Property(e => e.City).IsRequired().HasMaxLength(50);
                entity.Property(e => e.District).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Neighborhood).HasMaxLength(200);
                entity.HasIndex(e => new { e.ShopId, e.City, e.District })
                    .HasDatabaseName("IX_MobileShopServiceAreas_Shop_Location");
            });

            // ShopImage Configuration
            builder.Entity<ShopImage>(entity =>
            {
                entity.HasOne(si => si.Shop)
                    .WithMany(s => s.Images)
                    .HasForeignKey(si => si.ShopId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ShopImageTag Configuration
            builder.Entity<ShopImageTag>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);

                entity.HasOne(t => t.ShopImage)
                    .WithMany(i => i.Tags)
                    .HasForeignKey(t => t.ShopImageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ShopVideo Configuration
            builder.Entity<ShopVideo>(entity =>
            {
                entity.Property(e => e.Url).IsRequired();
                entity.HasOne(v => v.Shop)
                    .WithMany(s => s.Videos)
                    .HasForeignKey(v => v.ShopId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(v => new { v.ShopId, v.DisplayOrder })
                    .HasDatabaseName("IX_ShopVideos_Shop_Order");
            });

            // ShopClosureDate Configuration
            builder.Entity<ShopClosureDate>(entity =>
            {
                entity.HasOne(c => c.Shop)
                    .WithMany(s => s.ClosureDates)
                    .HasForeignKey(c => c.ShopId)
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
                entity.Property(e => e.Price).IsRequired().HasPrecision(18, 2);
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
                    .WithMany(se => se.Schedules)
                    .HasForeignKey(es => es.ShopEmployeeId)
                    .OnDelete(DeleteBehavior.Cascade); // Employee silinirse takvimi de silinsin.

                // Composite unique constraint might be good: One schedule per employee per day
                // builder.Entity<EmployeeSchedule>().HasIndex(es => new { es.ShopEmployeeId, es.DayOfWeek }).IsUnique(); 
                // But BaseEntity has Id, so it's fine. Application logic should ensure uniqueness.
            });

            // EmployeeLeaveDate Configuration
            builder.Entity<EmployeeLeaveDate>(entity =>
            {
                entity.HasOne(el => el.ShopEmployee)
                    .WithMany(se => se.LeaveDates)
                    .HasForeignKey(el => el.ShopEmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

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




            // UserFavoriteShop Configuration
            builder.Entity<UserFavoriteShop>(entity =>
            {
                entity.Property(e => e.CircleUserId).IsRequired();
                
                entity.HasOne(ufs => ufs.Shop)
                    .WithMany()
                    .HasForeignKey(ufs => ufs.ShopId)
                    .OnDelete(DeleteBehavior.Cascade); // Shop silinirse favoriler de silinsin
            });

            // Review Configuration
            builder.Entity<Review>(entity =>
            {
                entity.Property(e => e.Rating).IsRequired();
                entity.Property(e => e.Comment).HasMaxLength(1000);

                entity.HasOne(r => r.Appointment)
                    .WithMany()
                    .HasForeignKey(r => r.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict); // Appointment shouldn't be deleted easily if reviewed, or cascade? Restrict is safer.

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Shop)
                    .WithMany()
                    .HasForeignKey(r => r.ShopId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ShopEmployee)
                    .WithMany()
                    .HasForeignKey(r => r.ShopEmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ShopCategoryAssignment Configuration
            builder.Entity<ShopCategoryAssignment>(entity =>
            {
                entity.HasKey(x => new { x.ShopId, x.CategoryValue });
                entity.HasOne(x => x.Shop)
                    .WithMany(s => s.Categories)
                    .HasForeignKey(x => x.ShopId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SalonApplicationCategoryItem Configuration
            builder.Entity<SalonApplicationCategoryItem>(entity =>
            {
                entity.HasKey(x => new { x.ApplicationId, x.CategoryValue });
                entity.HasOne(x => x.Application)
                    .WithMany(a => a.Categories)
                    .HasForeignKey(x => x.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // RefreshToken Configuration
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.Token).IsRequired().HasMaxLength(256);
                entity.HasIndex(rt => rt.Token).IsUnique();
                entity.HasIndex(rt => new { rt.IsRevoked, rt.CreatedAt })
                    .HasDatabaseName("IX_RefreshTokens_Revoked_CreatedAt");
                entity.HasOne(rt => rt.User)
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OtpCode Configuration
            builder.Entity<OtpCode>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.PhoneNumber).IsRequired().HasMaxLength(15);
                entity.Property(o => o.CodeHash).IsRequired().HasMaxLength(64);
                entity.HasIndex(o => new { o.PhoneNumber, o.Purpose });
                entity.HasIndex(o => o.CreatedAt)
                    .HasDatabaseName("IX_OtpCodes_CreatedAt");
            });

            // ReviewImage Configuration
            builder.Entity<ReviewImage>(entity =>
            {
                entity.Property(e => e.Url).IsRequired();

                entity.HasOne(ri => ri.Review)
                    .WithMany(r => r.Images)
                    .HasForeignKey(ri => ri.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ShopBlockedCustomer Configuration
            builder.Entity<ShopBlockedCustomer>(entity =>
            {
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.HasOne(b => b.Shop)
                    .WithMany()
                    .HasForeignKey(b => b.ShopId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(b => b.Customer)
                    .WithMany()
                    .HasForeignKey(b => b.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(b => new { b.ShopId, b.CustomerId }).IsUnique();
            });

            // MediaLike Configuration
            builder.Entity<MediaLike>(entity =>
            {
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.MediaItemType).IsRequired().HasMaxLength(10);

                entity.HasOne(l => l.User)
                    .WithMany()
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Her kullanıcı bir medya öğesine yalnızca bir kez beğeni atabilir
                entity.HasIndex(l => new { l.UserId, l.MediaItemId, l.MediaItemType })
                    .IsUnique()
                    .HasDatabaseName("UQ_MediaLikes_User_Item");

                entity.HasIndex(l => new { l.MediaItemId, l.MediaItemType })
                    .HasDatabaseName("IX_MediaLikes_Item");
            });

            // ── Performance Indexes ────────────────────────────────────────────
            // Randevu çakışma kontrolü ve listeleme sorguları
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.ShopEmployeeId, a.Status, a.StartTime })
                .HasDatabaseName("IX_Appointments_Employee_Status_Time");

            builder.Entity<Appointment>()
                .HasIndex(a => new { a.UserId, a.StartTime })
                .HasDatabaseName("IX_Appointments_User_Time");

            builder.Entity<Appointment>()
                .HasIndex(a => a.GroupId)
                .HasDatabaseName("IX_Appointments_GroupId");

            // Çalışan listeleme ve kullanıcı-çalışan bağlantısı
            builder.Entity<ShopEmployee>()
                .HasIndex(se => new { se.ShopId, se.IsDeleted, se.IsActive })
                .HasDatabaseName("IX_ShopEmployees_Shop_Status");

            builder.Entity<ShopEmployee>()
                .HasIndex(se => new { se.UserId, se.IsDeleted })
                .HasDatabaseName("IX_ShopEmployees_User_Deleted");

            // Salon sahibi sorguları
            builder.Entity<Shop>()
                .HasIndex(s => new { s.OwnerId, s.IsActive })
                .HasDatabaseName("IX_Shops_Owner_Active");

            // Salon kodu benzersizliği (filtered: sadece NULL olmayan kodlar)
            builder.Entity<Shop>()
                .HasIndex(s => s.Code)
                .IsUnique()
                .HasFilter("[Code] IS NOT NULL")
                .HasDatabaseName("UQ_Shops_Code");

            // Yorum sorguları
            builder.Entity<Review>()
                .HasIndex(r => r.AppointmentId)
                .HasDatabaseName("IX_Reviews_AppointmentId");

            builder.Entity<Review>()
                .HasIndex(r => r.ShopId)
                .HasDatabaseName("IX_Reviews_ShopId");

            // Dashboard stats query indexes
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.ShopId, a.StartTime })
                .HasDatabaseName("IX_Appointments_ShopId_StartTime");

            builder.Entity<ShopService>()
                .HasIndex(s => new { s.ShopId, s.IsDeleted, s.IsActive })
                .HasDatabaseName("IX_ShopServices_ShopId_Status");

            builder.Entity<ShopEmployeeService>()
                .HasIndex(ses => ses.ShopEmployeeId)
                .HasDatabaseName("IX_ShopEmployeeServices_EmployeeId");

            builder.Entity<EmployeeSchedule>()
                .HasIndex(es => new { es.ShopEmployeeId, es.IsWorking })
                .HasDatabaseName("IX_EmployeeSchedules_Employee_IsWorking");

            // Randevu oluşturma ve slot hesaplama: es.ShopEmployeeId == X && es.DayOfWeek == Y
            builder.Entity<EmployeeSchedule>()
                .HasIndex(es => new { es.ShopEmployeeId, es.DayOfWeek })
                .HasDatabaseName("IX_EmployeeSchedules_Employee_DayOfWeek");

            // Yorum sorgulama: r.ShopEmployeeId == X (rating güncellemesi, her yorum eklemede çalışır)
            builder.Entity<Review>()
                .HasIndex(r => r.ShopEmployeeId)
                .HasDatabaseName("IX_Reviews_ShopEmployeeId");

            // Müşteri yorum sayfası: r.UserId == X
            builder.Entity<Review>()
                .HasIndex(r => r.UserId)
                .HasDatabaseName("IX_Reviews_UserId");

            // Background service (dakikada 1): süresi geçmiş Pending ve tamamlanacak Confirmed randevular
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.Status, a.EndTime })
                .HasDatabaseName("IX_Appointments_Status_EndTime");

            // Background service: 48 saat hatırlatması — Status=Confirmed, !Is48hReminderSent, StartTime aralığı
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.Status, a.Is48hReminderSent, a.StartTime })
                .HasDatabaseName("IX_Appointments_Reminder48h");

            // Background service: 2 saat hatırlatması — Status=Confirmed, !Is2hReminderSent, StartTime aralığı
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.Status, a.Is2hReminderSent, a.StartTime })
                .HasDatabaseName("IX_Appointments_Reminder2h");

            // Randevu oluşturma: çalışanın izin günü kontrolü
            builder.Entity<EmployeeLeaveDate>()
                .HasIndex(l => new { l.ShopEmployeeId, l.LeaveDate })
                .HasDatabaseName("IX_EmployeeLeaveDates_Employee_Date");

            // Randevu oluşturma ve slot listeleme: salonun kapalı gün kontrolü
            builder.Entity<ShopClosureDate>()
                .HasIndex(c => new { c.ShopId, c.ClosureDate })
                .HasDatabaseName("IX_ShopClosureDates_Shop_Date");

            // Favori toggle: CircleUserId + ShopId birlikte filtreleniyor
            builder.Entity<UserFavoriteShop>()
                .HasIndex(f => new { f.CircleUserId, f.ShopId })
                .HasDatabaseName("IX_UserFavoriteShops_User_Shop");
        }

        public DbSet<CoreExample> CoreExamples { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<KuaforumAPI.Domain.Common.BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = _dateTimeService.Now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = _dateTimeService.Now;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
