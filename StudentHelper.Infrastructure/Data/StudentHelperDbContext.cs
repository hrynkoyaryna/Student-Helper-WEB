using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Infrastructure.Data;

public class StudentHelperDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public StudentHelperDbContext(DbContextOptions<StudentHelperDbContext> options)
        : base(options)
    {
    }

    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ScheduleLesson> ScheduleLessons => Set<ScheduleLesson>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<PersonalEvent> PersonalEvents => Set<PersonalEvent>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Налаштування для Group
        modelBuilder.Entity<Group>()
            .HasMany(g => g.Users)
            .WithOne(u => u.Group)
            .HasForeignKey(u => u.GroupId)
            .IsRequired(false) 
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Group>()
            .HasMany(g => g.ScheduleLessons)
            .WithOne(s => s.Group)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Налаштування для Subject
        modelBuilder.Entity<Subject>()
            .HasMany(s => s.ScheduleLessons)
            .WithOne(sl => sl.Subject)
            .HasForeignKey(sl => sl.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Налаштування для Teacher
        modelBuilder.Entity<Teacher>()
            .HasMany(t => t.ScheduleLessons)
            .WithOne(sl => sl.Teacher)
            .HasForeignKey(sl => sl.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Teacher>()
            .HasMany(t => t.Exams)
            .WithOne(e => e.Teacher)
            .HasForeignKey(e => e.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Tasks)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Notes)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.PersonalEvents)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int> { Id = 1, Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole<int> { Id = 2, Name = "User", NormalizedName = "USER" }
        );
    }
}