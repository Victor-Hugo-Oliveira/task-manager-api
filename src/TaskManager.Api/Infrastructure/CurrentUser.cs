using Microsoft.IdentityModel.JsonWebTokens;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Interfaces;

namespace TaskManager.Api.Infrastructure;

/// <summary>Reads the authenticated user's id from the JWT "sub" claim.</summary>
public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id)
                ? id
                : throw new UnauthorizedException("User is not authenticated.");
        }
    }
}
