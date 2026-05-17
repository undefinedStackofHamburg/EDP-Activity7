// ============================================================
//  Controllers/LoansController.cs
//  ───────────────────────────────
//  Handles Loan transactions — the most important controller.
//  Base URL: /api/loans
//
//  ENDPOINTS:s
//  GET    /api/loans              → all loans with member & book names
//  GET    /api/loans/{id}         → single loan
//  POST   /api/loans              → create a new loan
//  POST   /api/loans/{id}/return  → process a book return + fine calc
//  DELETE /api/loans/{id}         → delete a returned loan record
//
//  BUSINESS RULES (same as original JS):
//  • Only books with available > 0 can be loaned
//  • Only Active members can borrow
//  • Fine = ₱5.00 × days late (calculated on return)
//  • Returning a book increments book.available by 1
// ============================================================

using Dapper;
using LibSys.API.Data;
using LibSys.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibSys.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly LibSysDbContext _db;

    // Fine rate — ₱5.00 per day overdue (matches JS logic)
    private const decimal FinePerDay = 5.00m;

    public LoansController(LibSysDbContext db) => _db = db;

    // ── GET /api/loans ───────────────────────────────────────
    // Returns all loans, JOINed with member name and book title
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _db.CreateConnection();

        var sql = @"
            SELECT
                l.id,
                l.book_id        AS BookId,
                l.member_id      AS MemberId,
                l.loan_date      AS LoanDate,
                l.due_date       AS DueDate,
                l.return_date    AS ReturnDate,
                l.fine_amount    AS FineAmount,
                l.status,
                CONCAT(m.first_name, ' ', m.last_name) AS MemberName,
                b.title                                 AS BookTitle
            FROM loans l
            JOIN members m ON m.id = l.member_id
            JOIN books   b ON b.id = l.book_id
            ORDER BY l.id DESC";

        var loans = await conn.QueryAsync<Loan>(sql);
        return Ok(ApiResponse<IEnumerable<Loan>>.Ok(loans));
    }

    // ── GET /api/loans/5 ─────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var conn = _db.CreateConnection();

        var sql = @"
            SELECT l.id, l.book_id AS BookId, l.member_id AS MemberId,
                   l.loan_date AS LoanDate, l.due_date AS DueDate,
                   l.return_date AS ReturnDate, l.fine_amount AS FineAmount, l.status,
                   CONCAT(m.first_name, ' ', m.last_name) AS MemberName,
                   b.title AS BookTitle
            FROM loans l
            JOIN members m ON m.id = l.member_id
            JOIN books   b ON b.id = l.book_id
            WHERE l.id = @Id";

        var loan = await conn.QueryFirstOrDefaultAsync<Loan>(sql, new { Id = id });

        if (loan is null)
            return NotFound(ApiResponse<Loan>.Fail($"Loan {id} not found."));

        return Ok(ApiResponse<Loan>.Ok(loan));
    }

    // ── POST /api/loans ──────────────────────────────────────
    // Creates a new loan.
    // Body: { "bookId": 1, "memberId": 2, "days": 14 }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLoanRequest req)
    {
        using var conn = _db.CreateConnection();

        // ── 1. Validate book exists and has copies available ──
        // NOTE: Dapper opens/closes the connection automatically for these
        //       plain queries, so no conn.OpenAsync() needed here yet.
        var book = await conn.QueryFirstOrDefaultAsync<Book>(
            "SELECT id, title, available FROM books WHERE id = @Id",
            new { Id = req.BookId });

        if (book is null)
            return BadRequest(ApiResponse<Loan>.Fail("Book not found."));

        if (book.Available <= 0)
            return BadRequest(ApiResponse<Loan>.Fail($"No available copies of '{book.Title}'."));

        // ── 2. Validate member exists and is Active ───────────
        var member = await conn.QueryFirstOrDefaultAsync<Member>(
            "SELECT id, status FROM members WHERE id = @Id",
            new { Id = req.MemberId });

        if (member is null)
            return BadRequest(ApiResponse<Loan>.Fail("Member not found."));

        if (member.Status != "Active")
            return BadRequest(ApiResponse<Loan>.Fail(
                $"Member is {member.Status} and cannot borrow books."));

        // ── 3. Create the loan record ─────────────────────────
        var loanDate = DateTime.Today;
        var dueDate  = loanDate.AddDays(req.Days > 0 ? req.Days : 14);

        // FIX: BeginTransactionAsync requires the connection to be open first.
        // Dapper's QueryAsync manages open/close internally, but ADO.NET-level
        // operations like BeginTransactionAsync do NOT — we must open manually.
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();
        try
        {
            // Insert loan
            var insertSql = @"
                INSERT INTO loans (book_id, member_id, loan_date, due_date, fine_amount, status)
                VALUES (@BookId, @MemberId, @LoanDate, @DueDate, 0.00, 'Active');
                SELECT LAST_INSERT_ID();";

            var newId = await conn.ExecuteScalarAsync<int>(insertSql, new
            {
                BookId   = req.BookId,
                MemberId = req.MemberId,
                LoanDate = loanDate,
                DueDate  = dueDate
            }, transaction);

            // Decrement available copies
            await conn.ExecuteAsync(
                "UPDATE books SET available = available - 1 WHERE id = @Id",
                new { Id = req.BookId }, transaction);

            await transaction.CommitAsync();

            return Created($"/api/loans/{newId}",
                ApiResponse<object>.Ok(new { id = newId, dueDate }, "Loan created successfully."));
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<Loan>.Fail("Failed to create loan. Please try again."));
        }
    }

    // ── POST /api/loans/5/return ─────────────────────────────
    // Processes a book return and calculates fine if overdue.
    // Body: { "loanId": 5, "returnDate": "2024-03-20" }
    [HttpPost("{id}/return")]
    public async Task<IActionResult> ReturnBook(int id, [FromBody] ReturnBookRequest req)
    {
        using var conn = _db.CreateConnection();

        // Plain query — Dapper handles open/close automatically
        var loan = await conn.QueryFirstOrDefaultAsync<Loan>(
            "SELECT id, book_id AS BookId, due_date AS DueDate, status FROM loans WHERE id = @Id",
            new { Id = id });

        if (loan is null)
            return NotFound(ApiResponse<Loan>.Fail($"Loan {id} not found."));

        if (loan.Status == "Returned")
            return BadRequest(ApiResponse<Loan>.Fail("This book has already been returned."));

        // ── Calculate fine ────────────────────────────────────
        // Fine = ₱5.00 × number of days past due date
        var returnDate = req.ReturnDate.Date;
        var daysLate   = (returnDate - loan.DueDate.Date).Days;
        var fine       = daysLate > 0 ? Math.Round(daysLate * FinePerDay, 2) : 0m;

        // FIX: Same as Create — must open before BeginTransactionAsync
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();
        try
        {
            // Update loan record
            await conn.ExecuteAsync(
                @"UPDATE loans
                  SET return_date = @ReturnDate,
                      fine_amount = @Fine,
                      status      = 'Returned'
                  WHERE id = @Id",
                new { Id = id, ReturnDate = returnDate, Fine = fine },
                transaction);

            // Increment available copies back
            await conn.ExecuteAsync(
                "UPDATE books SET available = available + 1 WHERE id = @BookId",
                new { BookId = loan.BookId },
                transaction);

            await transaction.CommitAsync();

            return Ok(ApiResponse<object>.Ok(
                new { fine, message = fine > 0 ? $"Fine charged: ₱{fine:F2}" : "Returned on time." },
                fine > 0 ? $"Book returned. Fine: ₱{fine:F2}" : "Book returned successfully."));
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiResponse<Loan>.Fail("Failed to process return. Please try again."));
        }
    }

    // ── DELETE /api/loans/5 ──────────────────────────────────
    // Deletes a RETURNED loan record (cleanup only)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = _db.CreateConnection();

        // Safety: only allow deletion of Returned loans
        var loan = await conn.QueryFirstOrDefaultAsync<Loan>(
            "SELECT id, status FROM loans WHERE id = @Id",
            new { Id = id });

        if (loan is null)
            return NotFound(ApiResponse<Loan>.Fail($"Loan {id} not found."));

        if (loan.Status != "Returned")
            return BadRequest(ApiResponse<Loan>.Fail(
                "Only returned loans can be deleted. Use the return endpoint first."));

        await conn.ExecuteAsync("DELETE FROM loans WHERE id = @Id", new { Id = id });

        return Ok(ApiResponse<object>.Ok(null, "Loan record deleted."));
    }
}
