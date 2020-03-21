using System.Collections.Generic;
using System.Linq;

namespace WebApiCore31
{
    public static class UserExtensions
    {
        public static User WithoutPassword(this User user)
        {
            user.Password = null;
            return user;
        }

        public static IEnumerable<User> WithoutPasswords(this IEnumerable<User> users)
        {
            return users.Select(x => WithoutPassword(x));
        }
    }
}