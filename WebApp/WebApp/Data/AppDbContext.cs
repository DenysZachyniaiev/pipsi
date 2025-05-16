using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<ClassStudents> ClassStudents => Set<ClassStudents>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ClassStudents>()
                .HasKey(sc => new { sc.StudentId, sc.ClassName });

            modelBuilder.Entity<ClassStudents>()
                .HasOne<Student>()
                .WithMany()
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassStudents>()
                .HasOne<Class>()
                .WithMany()
                .HasForeignKey(sc => sc.ClassName)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subject>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Subject>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Subject>()
                .HasOne<Class>()
                .WithMany()
                .HasForeignKey(s => s.ClassName)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScheduleEntry>()
                .HasKey(se => se.Id);

            modelBuilder.Entity<ScheduleEntry>()
                .HasOne<Subject>()
                .WithMany()
                .HasForeignKey(se => se.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}