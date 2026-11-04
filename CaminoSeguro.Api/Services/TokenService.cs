using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CaminoSeguro.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CaminoSeguro.Api.Services;

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "CaminoSeguro";
    public string Audience { get; set; } = "CaminoSeguroWeb";
    public int HorasExpiracion { get; set; } = 2;
}

public class TokenService(IOptions<JwtOptions> opciones)
{
    private readonly JwtOptions _o = opciones.Value;

    public (string Token, DateTime ExpiraEn) Crear(Usuario u)
    {
        var expira = DateTime.UtcNow.AddHours(_o.HorasExpiracion);

        // El correo NO va en el token: solo el id y el alias.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim("alias", u.Alias),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _o.Issuer,
            audience: _o.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
