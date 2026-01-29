using Microsoft.AspNetCore.Identity;

namespace Infraestrutura.EntityFramework
{
    public class ApplicationUser : IdentityUser
    {
        public int ClienteId { get; set; }
    }
}
