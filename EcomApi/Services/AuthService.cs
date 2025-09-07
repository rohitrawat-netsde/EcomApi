using EcomApi.DTOs;
using EcomApi.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text;
using System;
using EcomApi.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _db;
        private readonly IJwtService _jwt;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext db,
            IJwtService jwt)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _jwt = jwt;
        }

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash); // uppercase hex
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string ipAddress)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) is not null)
                throw new ApplicationException("Email already registered");

            var user = new ApplicationUser
            {
                Id = dto.Id,
                UserName = dto.Email,
                Email = dto.Email,
                Photo = dto.Photo,
                Gender = dto.Gender,
                Dob = dto.Dob
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new ApplicationException(string.Join(" ", result.Errors.Select(e => e.Description)));

            // default role "user" (optional): await _userManager.AddToRoleAsync(user, "user");

            var access = _jwt.GenerateAccessToken(user);

            var refreshRaw = _jwt.CreateRefreshTokenRaw();
            var rt = new RefreshToken
            {
                TokenHash = Sha256(refreshRaw),
                Expires = DateTime.UtcNow.AddDays(_jwt.GetRefreshTokenExpiryDays()),
                CreatedByIp = ipAddress,
                UserId = user.Id
            };
            _db.RefreshTokens.Add(rt);
            await _db.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = access,
                RefreshToken = refreshRaw,
                ExpiresInMinutes = _jwt.GetAccessTokenExpiryMinutes()
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) throw new ApplicationException("Invalid credentials");

            // This applies lockout policy automatically
            var signIn = await _signInManager.PasswordSignInAsync(user, dto.Password, isPersistent: false, lockoutOnFailure: true);
            if (signIn.IsLockedOut)
                throw new ApplicationException("Account locked due to multiple failed attempts. Try again later.");
            if (!signIn.Succeeded)
                throw new ApplicationException("Invalid credentials");

            var access = _jwt.GenerateAccessToken(user);

            var raw = _jwt.CreateRefreshTokenRaw();
            var rt = new RefreshToken
            {
                TokenHash = Sha256(raw),
                Expires = DateTime.UtcNow.AddDays(_jwt.GetRefreshTokenExpiryDays()),
                CreatedByIp = ipAddress,
                UserId = user.Id
            };
            _db.RefreshTokens.Add(rt);
            await _db.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = access,
                RefreshToken = raw,
                ExpiresInMinutes = _jwt.GetAccessTokenExpiryMinutes()
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshTokenRaw, string ipAddress)
        {
            var hash = Sha256(refreshTokenRaw);

            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(r =>
                r.TokenHash == hash && !r.IsRevoked && r.Expires > DateTime.UtcNow);

            if (existing == null) throw new ApplicationException("Invalid refresh token");

            // Rotate token: revoke old, issue new
            existing.IsRevoked = true;

            var user = await _userManager.FindByIdAsync(existing.UserId)
                       ?? throw new ApplicationException("User not found");

            var newRaw = _jwt.CreateRefreshTokenRaw();
            var newRt = new RefreshToken
            {
                TokenHash = Sha256(newRaw),
                Expires = DateTime.UtcNow.AddDays(_jwt.GetRefreshTokenExpiryDays()),
                CreatedByIp = ipAddress,
                UserId = user.Id
            };
            existing.ReplacedByTokenHash = newRt.TokenHash;

            _db.RefreshTokens.Add(newRt);
            await _db.SaveChangesAsync();

            var newAccess = _jwt.GenerateAccessToken(user);
            return new AuthResponseDto
            {
                AccessToken = newAccess,
                RefreshToken = newRaw,
                ExpiresInMinutes = _jwt.GetAccessTokenExpiryMinutes()
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshTokenRaw)
        {
            var hash = Sha256(refreshTokenRaw);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
            if (token == null) throw new ApplicationException("Invalid token");
            token.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }
}
