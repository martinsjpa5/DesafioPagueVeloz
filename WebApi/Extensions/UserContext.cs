using Application.Interfaces.Context;
using System.Security.Claims;

namespace WebApi.Extensions
{
    public class UserContext : IUserContext
    {
        public int ClienteId { get; }

        public UserContext(IHttpContextAccessor accessor)
        {
            var user = accessor.HttpContext?.User
                ?? throw new UnauthorizedAccessException();

            ClienteId = int.Parse(user.FindFirstValue("clienteId")!);
        }
    }

}
