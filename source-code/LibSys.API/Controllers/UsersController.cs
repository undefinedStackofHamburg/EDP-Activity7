// ============================================================
//  Controllers/UsersController.cs
//  ────────────────────────────────
//  Handles User Account management.
//  Base URL: /api/users
//
//  ENDPOINTS:
//  GET    /api/users              → list all accounts
//  GET    /api/users/{id}         → single account
//  POST   /api/users              → create account
//  PUT    /api/users/{id}         → update profile / status
//  POST   /api/users/login        → authenticate (login)
//  POST   /api/users/{id}/password → change password
// ============================================================

using Dapper;
using LibSys.API.Data;
using LibSys.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace LibSys.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly LibSysDbContext _db;
    public UsersController(LibSysDbContext db) => _db = db;

    // ── Hashes a plain-text password with SHA-256 ─────────────
    // Matches the SHA2('password', 256) function used in MySQL.
    private static string HashPassword(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes).ToLower();
    }

    // ── GET /api/users ────────────────────────────────────────
    // Returns all user accounts (password hash is never sent)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _db.CreateConnection();

        var users = await conn.QueryAsync<UserAccount>(
            @"SELECT id, username, full_name AS FullName, email,
                     role, status, created_at AS CreatedAt
              FROM users
              ORDER BY id");

        return Ok(ApiResponse<IEnumerable<UserAccount>>.Ok(users));
    }

    // ── GET /api/users/5 ──────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var conn = _db.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<UserAccount>(
            @"SELECT id, username, full_name AS FullName, email,
                     role, status, created_at AS CreatedAt
              FROM users WHERE id = @Id",
            new { Id = id });

        if (user is null)
            return NotFound(ApiResponse<UserAccount>.Fail($"User {id} not found."));

        return Ok(ApiResponse<UserAccount>.Ok(user));
    }

    // ── POST /api/users ───────────────────────────────────────
    // Creates a new account.
    // Body: { "username":"...", "fullName":"...", "email":"...",
    //         "role":"Admin|Staff", "password":"..." }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest(ApiResponse<UserAccount>.Fail("Username is required."));

        if (string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(ApiResponse<UserAccount>.Fail("Full name is required."));

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest(ApiResponse<UserAccount>.Fail("Password must be at least 6 characters."));

        var validRoles = new[] { "Admin", "Staff" };
        if (!validRoles.Contains(req.Role))
            return BadRequest(ApiResponse<UserAccount>.Fail("Role must be Admin or Staff."));

        using var conn = _db.CreateConnection();

        // Check username is unique — compare trimmed values in DB too
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM users WHERE TRIM(username) = @Username",
            new { Username = req.Username.Trim() });

        if (exists > 0)
            return Conflict(ApiResponse<UserAccount>.Fail("Username is already taken."));

        var sql = @"
            INSERT INTO users (username, full_name, email, role, password_hash, status)
            VALUES (@Username, @FullName, @Email, @Role, @Hash, 'Active');
            SELECT LAST_INSERT_ID();";

        var newId = await conn.ExecuteScalarAsync<int>(sql, new
        {
            Username = req.Username.Trim(),
            FullName = req.FullName.Trim(),
            Email    = req.Email?.Trim() ?? "",
            Role     = req.Role,
            Hash     = HashPassword(req.Password)
        });

        return Created($"/api/users/{newId}",
            ApiResponse<object>.Ok(new { id = newId }, "Account created."));
    }

    // ── PUT /api/users/5 ──────────────────────────────────────
    // Updates profile info and/or status.
    // Does NOT change the password — use /password for that.
    // Body: { "fullName":"...", "email":"...", "role":"...", "status":"..." }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
    {
        var validRoles    = new[] { "Admin", "Staff" };
        var validStatuses = new[] { "Active", "Inactive" };

        if (!validRoles.Contains(req.Role))
            return BadRequest(ApiResponse<UserAccount>.Fail("Role must be Admin or Staff."));

        if (!validStatuses.Contains(req.Status))
            return BadRequest(ApiResponse<UserAccount>.Fail("Status must be Active or Inactive."));

        using var conn = _db.CreateConnection();

        var rows = await conn.ExecuteAsync(
            @"UPDATE users
              SET full_name = @FullName,
                  email     = @Email,
                  role      = @Role,
                  status    = @Status
              WHERE id = @Id",
            new
            {
                Id       = id,
                FullName = req.FullName.Trim(),
                Email    = req.Email?.Trim() ?? "",
                Role     = req.Role,
                Status   = req.Status
            });

        if (rows == 0)
            return NotFound(ApiResponse<UserAccount>.Fail($"User {id} not found."));

        return Ok(ApiResponse<UserAccount>.Ok(null, "Account updated."));
    }

    // ── POST /api/users/login ─────────────────────────────────
    // Authenticates a user.
    // Body: { "username":"admin", "password":"password" }
    // Accepts username (with or without surrounding whitespace) OR email.
    // Passwords are sent plain-text from the browser; we hash server-side.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(ApiResponse<UserAccount>.Fail("Username and password are required."));

        using var conn = _db.CreateConnection();

        // Trim surrounding whitespace from the identifier before lookup.
        // The username stored in the DB may itself contain internal spaces
        // (e.g. "john doe") so we only trim the edges, not the middle.
        var identifier = req.Username.Trim();

        // Match by username (exact, case-sensitive) OR by email (case-insensitive).
        // The TRIM() in SQL handles any whitespace that crept into stored usernames.
        var user = await conn.QueryFirstOrDefaultAsync<UserAccount>(
            @"SELECT id, username, full_name AS FullName, email,
                     role, status, created_at AS CreatedAt
              FROM users
              WHERE (TRIM(username) = @Identifier
                     OR LOWER(TRIM(email)) = LOWER(@Identifier))
                AND password_hash = @Hash",
            new
            {
                Identifier = identifier,
                Hash       = HashPassword(req.Password)
            });

        if (user is null)
            return Unauthorized(ApiResponse<UserAccount>.Fail("Invalid username or password."));

        if (user.Status == "Inactive")
            return Unauthorized(ApiResponse<UserAccount>.Fail("This account has been deactivated."));

        return Ok(ApiResponse<UserAccount>.Ok(user, "Login successful."));
    }

    // ── POST /api/users/5/password ────────────────────────────
    // Changes the password for an account.
    // Body: { "currentPassword":"...", "newPassword":"..." }
    [HttpPost("{id}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CurrentPassword))
            return BadRequest(ApiResponse<UserAccount>.Fail("Current password is required."));

        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return BadRequest(ApiResponse<UserAccount>.Fail("New password must be at least 6 characters."));

        using var conn = _db.CreateConnection();

        // Verify current password first
        var correct = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM users WHERE id = @Id AND password_hash = @Hash",
            new { Id = id, Hash = HashPassword(req.CurrentPassword) });

        if (correct == 0)
            return BadRequest(ApiResponse<UserAccount>.Fail("Current password is incorrect."));

        var rows = await conn.ExecuteAsync(
            "UPDATE users SET password_hash = @Hash WHERE id = @Id",
            new { Id = id, Hash = HashPassword(req.NewPassword) });

        if (rows == 0)
            return NotFound(ApiResponse<UserAccount>.Fail($"User {id} not found."));

        return Ok(ApiResponse<UserAccount>.Ok(null, "Password changed successfully."));
    }
}
