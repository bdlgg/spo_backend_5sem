using Microsoft.EntityFrameworkCore;
using backend_spo.Entities;

namespace backend_spo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Car> Cars { get; set; }
        public DbSet<CarPhoto> CarPhotos { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role>  Roles { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<TestDrive> TestDrives { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.Cars).WithOne(c => c.Supplier).HasForeignKey(c => c.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users).WithOne(u => u.Role).HasForeignKey(r => r.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Sales).WithOne(s => s.User).HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Client>()
                .HasMany(c => c.Sales).WithOne(s => s.Client).HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Car>()
                .HasMany(c => c.Sales).WithOne(s => s.Car).HasForeignKey(s => s.CarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Car>()
                .HasIndex(c => c.Vin)
                .IsUnique();
            
            modelBuilder.Entity<CarPhoto>()
                .HasOne(p => p.Car)
                .WithMany(c => c.Photos)
                .HasForeignKey(p => p.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CarPhoto>()
                .Property(p => p.Url)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique(); 
            modelBuilder.Entity<User>()
                .HasMany(u => u.AuditLogs)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TestDrive>()
                .HasOne(td => td.Car)
                .WithMany()
                .HasForeignKey(td => td.CarId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestDrive>()
                .HasOne(td => td.Client)
                .WithMany()
                .HasForeignKey(td => td.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestDrive>()
                .HasOne(td => td.Manager)
                .WithMany()
                .HasForeignKey(td => td.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.Client)
                .WithMany()
                .HasForeignKey(so => so.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.Car)
                .WithMany()
                .HasForeignKey(so => so.CarId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.Manager)
                .WithMany()
                .HasForeignKey(so => so.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}