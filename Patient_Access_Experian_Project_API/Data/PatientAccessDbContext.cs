using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Data
{
    public class PatientAccessDbContext : DbContext
    {
        public PatientAccessDbContext(DbContextOptions<PatientAccessDbContext> options) : base(options) { }

        // Temporary table just to test migrations work end-to-end.
        //public DbSet<DemoEntity> DemoEntities => Set<DemoEntity>();

        public DbSet<Clinic> Clinics => Set<Clinic>();
        public DbSet<Provider> Providers => Set<Provider>();
        public DbSet<AvailabilityWindow> AvailabilityWindows => Set<AvailabilityWindow>();
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Appointment> Appointments => Set<Appointment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Clinic
            modelBuilder.Entity<Clinic>(entity =>
            {
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.Property(x => x.TimeZone).IsRequired().HasMaxLength(100);
            });

            // Provider

            modelBuilder.Entity<Provider>(entity =>
            {
                entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
                entity.Property(x => x.Specialty).IsRequired().HasMaxLength(200);

                // 1 Provider can have many AvailabilityWindows
                entity.HasMany(x => x.AvailabilityWindows)
                    .WithOne(x => x.Provider)
                    .HasForeignKey(x => x.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Availability Window

            modelBuilder.Entity<AvailabilityWindow>(entity =>
            {
                entity.Property(x => x.DayOfWeek).IsRequired();
                entity.Property(x => x.StartMinuteOfDay).IsRequired();
                entity.Property(x => x.EndMinuteOfDay).IsRequired();

                //Prevent duplicates
                entity.HasIndex(x => new { x.ProviderId, x.DayOfWeek, x.StartMinuteOfDay, x.EndMinuteOfDay })
                    .IsUnique();
            });

            // Patient

            modelBuilder.Entity<Patient>(entity =>
            {
                entity.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(x => x.LastName).IsRequired().HasMaxLength(100);

                entity.Property(x => x.Email).IsRequired().HasMaxLength(256);
                entity.Property(x => x.Phone).IsRequired().HasMaxLength(50);

                // Unique email
                entity.HasIndex(x => x.Email).IsUnique();
            });

            // Appointment 

            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.Property(x => x.StartUtc).IsRequired();
                entity.Property(x=>x.EndUtc).IsRequired();
                entity.Property(x => x.Status).IsRequired();
                entity.Property(x=>x.CreatedUtc).IsRequired();

                entity.HasOne(x => x.Clinic)
                .WithMany()
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Provider)
                .WithMany()
                .HasForeignKey(x => x.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Patient)
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

                // Indexes important for scheduling

                entity.HasIndex(x => new { x.ProviderId, x.StartUtc});
                entity.HasIndex(x => new { x.ClinicId, x.StartUtc });

            });

            // Seed Data
            var clinicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var provider1Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var provider2Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var provider3Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var patientId = Guid.Parse("55555555-5555-5555-5555-555555555555");

            // Clinic Seed Data
            modelBuilder.Entity<Clinic>().HasData(new Clinic
            {
                Id = clinicId,
                Name = "Health Clinic A",
                TimeZone = "America/Chicago"
            });
            // Paient Seed Data
            modelBuilder.Entity<Patient>().HasData(new Patient
            {
                Id = patientId,
                FirstName = "Alex",
                LastName = "Johnson",
                Email = "alex.johnson@gmail.com",
                Phone = "555-111-2222"
            });

            // Healthcare Provider Seed Data
            modelBuilder.Entity<Provider>().HasData(
                new Provider { Id = provider1Id, Name = "Dr. Maya Patel", Specialty = "Primary Care" },
                new Provider { Id = provider2Id, Name = "Dr. Jordan Lee", Specialty = "Cardiology" },
                new Provider { Id = provider3Id, Name = "Dr. Sofia Ramirez", Specialty = "Dermatology" }
                );

            // Availability: Mon-Fri 9am-5pm (in minutes)
            int start = 9 * 60;
            int end = 17 * 60;

            modelBuilder.Entity<AvailabilityWindow>().HasData(
                // Dr. Maya Patel Availability
                new AvailabilityWindow { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), ProviderId = provider1Id, DayOfWeek = 1, StartMinuteOfDay = start, EndMinuteOfDay = end },
                new AvailabilityWindow { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), ProviderId = provider1Id, DayOfWeek = 2, StartMinuteOfDay = start, EndMinuteOfDay = end },
                new AvailabilityWindow { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), ProviderId = provider1Id, DayOfWeek = 3, StartMinuteOfDay = start, EndMinuteOfDay = end },
                new AvailabilityWindow { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), ProviderId = provider1Id, DayOfWeek = 4, StartMinuteOfDay = start, EndMinuteOfDay = end },
                new AvailabilityWindow { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), ProviderId = provider1Id, DayOfWeek = 5, StartMinuteOfDay = start, EndMinuteOfDay = end },
                // Dr. Jordan Lee Availability
                new AvailabilityWindow { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), ProviderId = provider2Id, DayOfWeek = 1, StartMinuteOfDay = start, EndMinuteOfDay = end },
                new AvailabilityWindow { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), ProviderId = provider2Id, DayOfWeek = 3, StartMinuteOfDay = start, EndMinuteOfDay = end },
                new AvailabilityWindow { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"), ProviderId = provider2Id, DayOfWeek = 5, StartMinuteOfDay = start, EndMinuteOfDay = end },
                // Dr. Sofia Ramirez Availability 
                new AvailabilityWindow { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"), ProviderId = provider3Id, DayOfWeek = 2, StartMinuteOfDay = start, EndMinuteOfDay = end },
                new AvailabilityWindow { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"), ProviderId = provider3Id, DayOfWeek = 4, StartMinuteOfDay = start, EndMinuteOfDay = end }
                );
        }
    }
}
