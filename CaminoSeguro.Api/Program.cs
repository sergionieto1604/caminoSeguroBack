using System.Text;
using System.Threading.RateLimiting;
using CaminoSeguro.Api.Data;
using CaminoSeguro.Api.Models;
using CaminoSeguro.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Configuración ----------
var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException(
        "Falta Jwt:Key o tiene menos de 32 caracteres. Configúrala con user-secrets o la variable Jwt__Key.");
builder.Services.Configure<JwtOptions>(config.GetSection("Jwt"));

var conexion = config.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:Default.");

// Acepta lista (Cors:AllowedOrigins:0, :1...) o texto separado por comas (Cors__AllowedOrigins=a,b).
var origenes = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (origenes is null || origenes.Length == 0)
    origenes = (config["Cors:AllowedOrigins"] ?? "http://localhost:4200")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var userAgentNominatim = config["Nominatim:UserAgent"] ?? "CaminoSeguro-Hackaton/1.0";

// ---------- Servicios ----------
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(conexion, sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<TokenService>();

builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(c =>
{
    c.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    c.Timeout = TimeSpan.FromSeconds(5);
    c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgentNominatim);
    c.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "es");
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // conserva "sub" tal cual
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "alias"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, ct) =>
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Demasiados intentos. Espera un minuto e inténtalo de nuevo." }, ct);

    // Login y registro: 10 por minuto por IP (frena fuerza bruta).
    o.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    // Crear/editar/confirmar: 20 por minuto por usuaria (frena spam de reportes).
    o.AddPolicy("escritura", http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.FindFirst("sub")?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Camino Seguro API", Version = "v1" });
    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega solo el token (sin la palabra Bearer).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", esquema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { esquema, Array.Empty<string>() } });
});

var app = builder.Build();

// ---------- Base de datos: crea tablas y semilla si no existen ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Usuaria demo solo en desarrollo (la misma del modo demo del front).
    if (app.Environment.IsDevelopment() && !db.Usuarios.Any(u => u.Email == "demo@demo.com"))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var demo = new Usuario { Email = "demo@demo.com", Alias = "Demo" };
        demo.PasswordHash = hasher.HashPassword(demo, "Demo1234");
        db.Usuarios.Add(demo);
        db.SaveChanges();
    }
}

// ---------- Pipeline ----------
app.UseExceptionHandler(e => e.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await ctx.Response.WriteAsJsonAsync(new { message = "Ocurrió un error inesperado. Intenta de nuevo." });
}));

if (app.Environment.IsDevelopment() || config.GetValue<bool>("Swagger:Habilitado"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/salud", () => Results.Ok(new { estado = "ok", hora = DateTime.UtcNow }));

app.Run();
