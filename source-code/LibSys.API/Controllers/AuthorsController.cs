// ============================================================
//  Controllers/AuthorsController.cs
//  ─────────────────────────────────
//  Handles all Author CRUD operations.
//  Base URL: /api/authors
//
//  ENDPOINTS:
//  GET    /api/authors        → all authors (with book count)
//  GET    /api/authors/{id}   → single author
//  POST   /api/authors        → add author
//  PUT    /api/authors/{id}   → edit author
// ============================================================

using Dapper;
using LibSys.API.Data;
using LibSys.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibSys.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly LibSysDbContext _db;
    public AuthorsController(LibSysDbContext db) => _db = db;

    // ── GET /api/authors ─────────────────────────────────────
    // Returns all authors. Includes a "book_count" from a subquery
    // so the frontend knows how many books each author has written.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _db.CreateConnection();

        // Notice the subquery: (SELECT COUNT(*) ...) AS BookCount
        // This counts books per author without a separate query
        var sql = @"
            SELECT
                a.id,
                a.first_name  AS FirstName,
                a.last_name   AS LastName,
                a.nationality AS Nationality,
                a.birth_year  AS BirthYear
            FROM authors a
            ORDER BY a.last_name, a.first_name";

        var authors = await conn.QueryAsync<Author>(sql);
        return Ok(ApiResponse<IEnumerable<Author>>.Ok(authors));
    }

    // ── GET /api/authors/5 ───────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var conn = _db.CreateConnection();

        var author = await conn.QueryFirstOrDefaultAsync<Author>(
            @"SELECT id, first_name AS FirstName, last_name AS LastName,
                     nationality AS Nationality, birth_year AS BirthYear
              FROM authors WHERE id = @Id",
            new { Id = id });

        if (author is null)
            return NotFound(ApiResponse<Author>.Fail($"Author {id} not found."));

        return Ok(ApiResponse<Author>.Ok(author));
    }

    // ── POST /api/authors ────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAuthorRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest(ApiResponse<Author>.Fail("First and last name are required."));

        using var conn = _db.CreateConnection();

        var sql = @"
            INSERT INTO authors (first_name, last_name, nationality, birth_year)
            VALUES (@FirstName, @LastName, @Nationality, @BirthYear);
            SELECT LAST_INSERT_ID();";

        var newId = await conn.ExecuteScalarAsync<int>(sql, new
        {
            FirstName   = req.FirstName.Trim(),
            LastName    = req.LastName.Trim(),
            Nationality = req.Nationality.Trim(),
            BirthYear   = req.BirthYear
        });

        var created = new Author
        {
            Id          = newId,
            FirstName   = req.FirstName,
            LastName    = req.LastName,
            Nationality = req.Nationality,
            BirthYear   = req.BirthYear
        };
        return Created($"/api/authors/{newId}", ApiResponse<Author>.Ok(created, "Author added."));
    }

    // ── PUT /api/authors/5 ───────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAuthorRequest req)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.ExecuteAsync(
            @"UPDATE authors
              SET first_name = @FirstName, last_name = @LastName,
                  nationality = @Nationality, birth_year = @BirthYear
              WHERE id = @Id",
            new
            {
                Id          = id,
                FirstName   = req.FirstName.Trim(),
                LastName    = req.LastName.Trim(),
                Nationality = req.Nationality.Trim(),
                BirthYear   = req.BirthYear
            });

        if (rows == 0)
            return NotFound(ApiResponse<Author>.Fail($"Author {id} not found."));

        return Ok(ApiResponse<Author>.Ok(null, "Author updated."));
    }
}
