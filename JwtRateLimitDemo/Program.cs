using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1) JWT Authentication + Authorization
builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(opts =>
  {
      var cfg = builder.Configuration.GetSection("Jwt");
      opts.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer           = true,
          ValidateAudience         = true,
          ValidateLifetime         = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer              = cfg["Issuer"],
          ValidAudience            = cfg["Audience"],
          IssuerSigningKey         = new SymmetricSecurityKey(
              Encoding.UTF8.GetBytes(cfg["Key"]!)),
          NameClaimType            = ClaimTypes.NameIdentifier,
          RoleClaimType            = ClaimTypes.Role
      };
  });
builder.Services.AddAuthorization();

// 2) Rate-Limiting Policy
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    
    options.AddPolicy("UserBasedRateLimit", httpContext =>
    {
        var user    = httpContext.User;
        var userKey = user.Identity?.Name ?? "anonymous";
        // define "premium" however you like:
        var isPremium = user.IsInRole("Premium")
                     || user.HasClaim("premium", "true");

        if (isPremium)
        {
            // Token Bucket for Premium
            return RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: userKey,
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit        = 100,
                    TokensPerPeriod   = 20,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    AutoReplenishment  = true,
                    QueueLimit        = 0
                });
        }
        else
        {
            // Concurrency limit for everyone else
            return RateLimitPartition.GetConcurrencyLimiter(
                partitionKey: userKey,
                factory: _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 1,
                    QueueLimit  = 2
                });
        }
    });
});

var app = builder.Build();

// 3) Middleware order
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// 4) Login endpoint (issues JWT)
app.MapPost("/login", (LoginRequest creds) =>
{
    // simple in-memory check:
    if ((creds.Username, creds.Password) switch
        {
            ("premium", "pass") => true,
            ("user",    "pass") => true,
            _                   => false
        } is false)
    {
        return Results.Unauthorized();
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, creds.Username),
        // mark premium users:
        new Claim("premium", creds.Username == "premium" ? "true" : "false")
    };
    if (creds.Username == "premium")
        claims.Add(new Claim(ClaimTypes.Role, "Premium"));

    var cfg = builder.Configuration.GetSection("Jwt");
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Key"]!));
    var credsSig = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddMinutes(30);

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer:              cfg["Issuer"],
        audience:            cfg["Audience"],
        claims:              claims,
        expires:             expires,
        signingCredentials:  credsSig
    );

    var tokenStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                       .WriteToken(token);

    return Results.Ok(new { token = tokenStr, expires });
})
.AllowAnonymous();

// 5) Protected + rate-limited endpoint
app.MapGet("/data", () => "🎉 Success! You hit /data.")
   .RequireAuthorization()
   .RequireRateLimiting("UserBasedRateLimit");

app.Run();

// DTO
record LoginRequest(string Username, string Password);
