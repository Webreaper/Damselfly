using Damselfly.Core.DbModels.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.Enums
{
    // TODO: auto sync with DB
    public enum RoleEnum
    {
        User = 1,
        Admin = 2,
        Readonly = 3
    }

    public static class RoleEnumExtensions
    {
        public static string ToFriendlyString(this RoleEnum role)
        {
            return role switch
            {
                RoleEnum.User => RoleDefinitions.s_UserRole,
                RoleEnum.Admin => RoleDefinitions.s_AdminRole,
                RoleEnum.Readonly => RoleDefinitions.s_ReadOnlyRole,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static RoleEnum FromFriendlyString(string role)
        {
            return role switch
            {
                RoleDefinitions.s_UserRole => RoleEnum.User,
                RoleDefinitions.s_AdminRole => RoleEnum.Admin,
                RoleDefinitions.s_ReadOnlyRole => RoleEnum.Readonly,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
