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
        }

    }
}