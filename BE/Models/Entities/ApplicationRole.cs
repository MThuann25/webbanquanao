using Microsoft.AspNetCore.Identity;

namespace ClothingShop.Domain.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base() { }
        public ApplicationRole(string roleName) : base(roleName) { }
    }
}
