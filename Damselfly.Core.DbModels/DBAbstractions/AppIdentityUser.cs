using Microsoft.AspNetCore.Identity;
using Damselfly.Core.Interfaces;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System;

namespace Damselfly.Core.DbModels
{
    public partial class AppIdentityUser : IdentityUser<int>, IDamselflyUser
    {
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }

    public partial class ApplicationRole : IdentityRole<int>
    {
        public ApplicationRole() : base()
        {

        }

        public ApplicationRole(string roleName)
        {
            Name = roleName;
        }

        public ICollection<AppIdentityUser> AspNetUsers { get; set; }
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
    }

    public partial class ApplicationUserRole : IdentityUserRole<int>
    {
        public virtual AppIdentityUser User { get; set; }
        public virtual ApplicationRole Role { get; set; }
    }

    public class PolicyDefinitions
    {
        public const string s_IsEditor = "IsEditor";
        public const string s_IsDownloader = "IsDownloader";
        public const string s_IsAdmin = "IsAdmin";
        public const string s_IsLoggedIn = "IsLoggedIn";
    }

    public class RoleDefinitions
    {
        // Role names
        public const string s_AdminRole = "Admin";
        public const string s_UserRole = "User";
        public const string s_ReadOnlyRole = "ReadOnly";

        /// <summary>
        /// Seed the roles for the application.
        /// </summary>
        /// <param name="modelBuilder"></param>
        private static void SeedRoles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole { Name = s_UserRole, NormalizedName = s_UserRole.ToUpper(), Id = 1, ConcurrencyStamp = Guid.NewGuid().ToString() });
            modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole { Name = s_AdminRole, NormalizedName = s_AdminRole.ToUpper(), Id = 2, ConcurrencyStamp = Guid.NewGuid().ToString() });
            modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole { Name = s_ReadOnlyRole, NormalizedName = s_ReadOnlyRole.ToUpper(), Id = 3, ConcurrencyStamp = Guid.NewGuid().ToString() });
        }

        /// <summary>
        /// https://github.com/aspnet/Identity/issues/1361#issuecomment-348863959
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

            SeedRoles(builder);
        }
    }
}
