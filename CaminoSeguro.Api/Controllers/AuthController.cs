using CaminoSeguro.Api.Data;
using CaminoSeguro.Api.Dtos;
using CaminoSeguro.Api.Models;
using CaminoSeguro.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CaminoSeguro.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(AppDbContext db, TokenService tokens, IPasswordHasher<Usuario> hasher) : ControllerBase
{
    [HttpPost("registro")]
    public async Task<ActionResult<AuthResponse>> Registro(RegistroRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var alias = req.Alias.Trim();

        if (alias.Length < 3)
            return BadRequest(new { message = "El alias debe tener entre 3 y 30 caracteres." });

        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { message = "Ya existe una cuenta con ese correo." });

        var usuario = new Usuario { Email = email, Alias = alias };
        usuario.PasswordHash = hasher.HashPassword(usuario, req.Password); // PBKDF2 con sal

        db.Usuarios.Add(usuario);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Dos registros simultáneos con el mismo correo: lo frena el índice único.
            return Conflict(new { message = "Ya existe una cuenta con ese correo." });
        }

        return StatusCode(StatusCodes.Status201Created, Respuesta(usuario));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (usuario is null)
        {
            // Se calcula un hash igual para que el tiempo de respuesta no revele si el correo existe.
            hasher.HashPassword(new Usuario(), req.Password);
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });
        }

        var resultado = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, req.Password);
        if (resultado == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Correo o contraseña incorrectos." });

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = hasher.HashPassword(usuario, req.Password);
            await db.SaveChangesAsync(ct);
        }

        return Ok(Respuesta(usuario));
    }

    private AuthResponse Respuesta(Usuario u)
    {
        var (token, expira) = tokens.Crear(u);
        return new AuthResponse { Token = token, ExpiraEn = expira, Alias = u.Alias, Email = u.Email };
    }
}
