// ============================================================
//  Controllers/CategoriesController.cs
//  ─────────────────────────────────────
//  What is this?
//  A Controller handles incoming HTTP requests from the browser
//  and returns HTTP responses (JSON data).
//
//  This controller handles everything about CATEGORIES.
//
//  The URL pattern is:  http://localhost:5000/api/categories
//
//  ENDPOINTS (what URLs do what):
//  ┌──────────────────────────────────────────────────────────┐
//  │ GET    /api/categories        → list all categories      │
//  │ GET    /api/categories/{id}   → get one category by ID   │
//  │ POST   /api/categories        → create a new category    │
//  │ PUT    /api/categories/{id}   → update a category        │
//  └──────────────────────────────────────────────────────────┘
// ============================================================

using Dapper;
using LibSys.API.Data;
using LibSys.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibSys.API.Controllers;

// [ApiController]  → enables automatic model validation & JSON responses
// [Route(...)]     → sets the base URL path for all methods in this class
[ApiController]
[Route("api/[controller]")]  // resolves to: api/categories
public class CategoriesController : ControllerBase
{
    // Dependency injection — DbContext is provided automatically by Program.cs
    private readonly LibSysDbContext _db;

    public CategoriesController(LibSysDbContext db)
    {
        _db = db;
    }

    // ── GET /api/categories ──────────────────────────────────
    // Returns all categories as JSON array
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _db.CreateConnection();

        // Dapper's QueryAsync runs the SQL and maps each row → Category object
        var categories = await conn.QueryAsync<Category>(
            "SELECT id, name, description FROM categories ORDER BY name");

        return Ok(ApiResponse<IEnumerable<Category>>.Ok(categories));
    }

    // ── GET /api/categories/5 ───────────────────────────────
    // Returns one category (or 404 if not found)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var conn = _db.CreateConnection();

        // @Id is a Dapper parameter — prevents SQL injection
        var category = await conn.QueryFirstOrDefaultAsync<Category>(
            "SELECT id, name, description FROM categories WHERE id = @Id",
            new { Id = id });

        if (category is null)
            return NotFound(ApiResponse<Category>.Fail($"Category {id} not found."));

        return Ok(ApiResponse<Category>.Ok(category));
    }

    // ── POST /api/categories ─────────────────────────────────
    // Creates a new category. Body: { "name": "...", "description": "..." }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(ApiResponse<Category>.Fail("Category name is required."));

        using var conn = _db.CreateConnection();

        // INSERT and return the new auto-generated ID using LAST_INSERT_ID()
        var sql = @"
            INSERT INTO categories (name, description)
            VALUES (@Name, @Description);
            SELECT LAST_INSERT_ID();";

        var newId = await conn.ExecuteScalarAsync<int>(sql, new
        {
            Name        = req.Name.Trim(),
            Description = req.Description.Trim()
        });

        var created = new Category { Id = newId, Name = req.Name, Description = req.Description };
        return Created($"/api/categories/{newId}", ApiResponse<Category>.Ok(created, "Category created."));
    }

    // ── PUT /api/categories/5 ────────────────────────────────
    // Updates an existing category
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCategoryRequest req)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.ExecuteAsync(
            "UPDATE categories SET name = @Name, description = @Description WHERE id = @Id",
            new { Id = id, Name = req.Name.Trim(), Description = req.Description.Trim() });

        if (rows == 0)
            return NotFound(ApiResponse<Category>.Fail($"Category {id} not found."));

        return Ok(ApiResponse<Category>.Ok(null, "Category updated."));
    }
}
