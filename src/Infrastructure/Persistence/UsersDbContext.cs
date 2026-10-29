using portfolio_users_rest_api.Application.Common;
using portfolio_users_rest_api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace portfolio_users_rest_api.Infrastructure.Persistence;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<LoginLogs> LoginLogs { get; set; }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The record was modified by another request. Refresh the resource and retry.", exception);
        }
        catch (DbUpdateException exception)
        {
            throw new PersistenceConflictException(
                "The request could not be saved because it conflicts with the current database state.", exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.UserPass)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.UserRoleId).IsRequired();
            entity.Property(e => e.DateCreated).HasColumnType("datetime");
            entity.Property(e => e.DateModified).HasColumnType("datetime");
            entity.HasOne(e => e.UserRole)
                .WithMany()
                .HasForeignKey(e => e.UserRoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserName).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");

            entity.HasKey(e => e.UserRoleId);

            entity.Property(e => e.RoleName)
                .HasColumnName("UserRole")
                .HasMaxLength(100)
                .IsRequired();
        });

        modelBuilder.Entity<LoginLogs>(entity =>
        {
            entity.ToTable("LoginLogs", "dbo");

            entity.HasKey(e => e.LogId);

            entity.Property(e => e.LogId).UseIdentityColumn();
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.LoginTime).HasColumnType("datetime2(7)");
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsRequired();
            entity.Property(e => e.UserAgent)
                .HasMaxLength(1024)
                .IsRequired();
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000)
                .IsRequired();
        });
    }
}
