using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Core.DbModels.Authentication;

public static class RoleDefinitions
{
    // Role names
    public const string s_AdminRole = "Admin";
    public const string s_UserRole = "User";
    public const string s_ReadOnlyRole = "ReadOnly";

    /// <summary>
    ///     https://github.com/aspnet/Identity/issues/1361#issuecomment-348863959
    /// </summary>
    /// <param name="builder"></param>
    public static void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AppIdentityUser>(b =>
        {
            b.ToTable("Users");
            b.HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();
        });

        builder.Entity<ApplicationRole>(role =>
        {
            role.ToTable("Roles");
            role.HasKey(r => r.Id);
            role.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();
            role.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

            role.Property(u => u.Name).HasMaxLength(256);
            role.Property(u => u.NormalizedName).HasMaxLength(256);

            role.HasMany<ApplicationUserRole>()
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();
            role.HasMany<IdentityRoleClaim<int>>()
                .WithOne()
                .HasForeignKey(rc => rc.RoleId)
                .IsRequired();
        });

        builder.Entity<IdentityRoleClaim<int>>(roleClaim =>
        {
            roleClaim.HasKey(rc => rc.Id);
            roleClaim.ToTable("RoleClaims");
        });

        builder.Entity<ApplicationUserRole>(userRole =>
        {
            userRole.ToTable("UserRoles");
            userRole.HasKey(r => new { r.UserId, r.RoleId });
        });

        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        builder.SeedRoles();
    }

    /// <summary>
    ///     Seed the roles for the application.
    /// </summary>
    /// <param name="modelBuilder"></param>
    private static void SeedRoles(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole
        {
            Name = s_UserRole, NormalizedName = s_UserRole.ToUpper(), Id = 1,
            ConcurrencyStamp = "79f29160-cfbe-40f8-ada7-c7672a5cb8a3"
        });
        modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole
        {
            Name = s_AdminRole, NormalizedName = s_AdminRole.ToUpper(), Id = 2,
            ConcurrencyStamp = "3a63af71-3be5-4c31-b692-c8f4bf281e50"
        });
        modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole
        {
            Name = s_ReadOnlyRole, NormalizedName = s_ReadOnlyRole.ToUpper(), Id = 3,
            ConcurrencyStamp = "cc37f753-44de-4766-92d1-eca84cc8be8e"
        });
    }
}