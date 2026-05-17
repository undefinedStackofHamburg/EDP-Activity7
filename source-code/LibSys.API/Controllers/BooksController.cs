// ============================================================
//  Controllers/BooksController.cs
//  ───────────────────────────────
//  Handles Book catalog CRUD.
//  Base URL: /api/books
//
//  ENDPOINTS:
//  GET    /api/books          → all books (with author & category names)
//  GET    /api/books/{id}     → single book
//  POST   /api/books          → add book
//  PUT    /api/books/{id}     → update book
// ============================================================

using Dapper;
using LibSys.API.Data;
using LibSys.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibSys.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly LibSysDbContext _db;
    public BooksController(LibSysDbContext db) => _db = db;

    // ── GET /api/books ───────────────────────────────────────
    // JOINs authors and categories so the frontend gets author
    // name and category name without extra requests.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _db.CreateConnection();

        // JOIN query: combines books + authors + categories in one SQL call
        var sql = @"
            SELECT
                b.id,
                b.title,
                b.author_id   AS AuthorId,
                b.cat_id      AS CatId,
                b.isbn,
                b.year_pub    AS YearPub,
                b.total,
                b.available,
                CONCAT(a.first_name, ' ', a.last_name) AS AuthorName,
                c.name                                  AS CategoryName
            FROM books b
            JOIN authors    a ON a.id = b.author_id
            JOIN categories c ON c.id = b.cat_id
            ORDER BY b.title";

        var books = await conn.QueryAsync<Book>(sql);
        return Ok(ApiResponse<IEnumerable<Book>>.Ok(books));
    }

    // ── GET /api/books/5 ─────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var conn = _db.CreateConnection();

        var sql = @"
            SELECT b.id, b.title, b.author_id AS AuthorId, b.cat_id AS CatId,
                   b.isbn, b.year_pub AS YearPub, b.total, b.available,
                   CONCAT(a.first_name, ' ', a.last_name) AS AuthorName,
                   c.name AS CategoryName
            FROM books b
            JOIN authors    a ON a.id = b.author_id
            JOIN categories c ON c.id = b.cat_id
            WHERE b.id = @Id";

        var book = await conn.QueryFirstOrDefaultAsync<Book>(sql, new { Id = id });

        if (book is null)
            return NotFound(ApiResponse<Book>.Fail($"Book {id} not found."));

        return Ok(ApiResponse<Book>.Ok(book));
    }

    // ── POST /api/books ──────────────────────────────────────
    // When a new book is added, available = total (all copies free)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(ApiResponse<Book>.Fail("Book title is required."));

        using var conn = _db.CreateConnection();

        var sql = @"
            INSERT INTO books (title, author_id, cat_id, isbn, year_pub, total, available)
            VALUES (@Title, @AuthorId, @CatId, @Isbn, @YearPub, @Total, @Total);
            SELECT LAST_INSERT_ID();";

        // Note: available = total on creation (new stock = all copies available)
        var newId = await conn.ExecuteScalarAsync<int>(sql, new
        {
            Title    = req.Title.Trim(),
            AuthorId = req.AuthorId,
            CatId    = req.CatId,
            Isbn     = req.Isbn.Trim(),
            YearPub  = req.YearPub,
            Total    = req.Total
        });

        return Created($"/api/books/{newId}",
            ApiResponse<object>.Ok(new { id = newId }, "Book added."));
    }

    // ── PUT /api/books/5 ─────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBookRequest req)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.ExecuteAsync(
            @"UPDATE books
              SET title = @Title, author_id = @AuthorId, cat_id = @CatId,
                  isbn = @Isbn, year_pub = @YearPub, total = @Total,
                  available = @Available
              WHERE id = @Id",
            new
            {
                Id        = id,
                Title     = req.Title.Trim(),
                AuthorId  = req.AuthorId,
                CatId     = req.CatId,
                Isbn      = req.Isbn.Trim(),
                YearPub   = req.YearPub,
                Total     = req.Total,
                Available = req.Available
            });

        if (rows == 0)
            return NotFound(ApiResponse<Book>.Fail($"Book {id} not found."));

        return Ok(ApiResponse<Book>.Ok(null, "Book updated."));
    }
}
