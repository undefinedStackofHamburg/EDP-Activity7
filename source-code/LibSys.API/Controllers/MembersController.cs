// ============================================================
//  Controllers/MembersController.cs
//  ─────────────────────────────────
//  Handles Member CRUD.
//  Base URL: /api/members
//
//  ENDPOINTS:
//  GET    /api/members        → all members
//  GET    /api/members/{id}   → single member
//  POST   /api/members        → register new member
//  PUT    /api/members/{id}   → update member info/status
// ============================================================

using Dapper;
using LibSys.API.Data;
using LibSys.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibSys.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly LibSysDbContext _db;
    public MembersController(LibSysDbContext db) => _db = db;

    // ── GET /api/members ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _db.CreateConnection();

        var members = await conn.QueryAsync<Member>(
            @"SELECT id, first_name AS FirstName, last_name AS LastName,
                     phone, membership_date AS MembershipDate, status
              FROM members
              ORDER BY last_name, first_name");

        return Ok(ApiResponse<IEnumerable<Member>>.Ok(members));
    }

    // ── GET /api/members/5 ───────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        using var conn = _db.CreateConnection();

        var member = await conn.QueryFirstOrDefaultAsync<Member>(
            @"SELECT id, first_name AS FirstName, last_name AS LastName,
                     phone, membership_date AS MembershipDate, status
              FROM members WHERE id = @Id",
            new { Id = id });

        if (member is null)
            return NotFound(ApiResponse<Member>.Fail($"Member {id} not found."));

        return Ok(ApiResponse<Member>.Ok(member));
    }

    // ── POST /api/members ────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest(ApiResponse<Member>.Fail("First and last name are required."));

        using var conn = _db.CreateConnection();

        var sql = @"
            INSERT INTO members (first_name, last_name, phone, membership_date, status)
            VALUES (@FirstName, @LastName, @Phone, @MembershipDate, 'Active');
            SELECT LAST_INSERT_ID();";

        var newId = await conn.ExecuteScalarAsync<int>(sql, new
        {
            FirstName      = req.FirstName.Trim(),
            LastName       = req.LastName.Trim(),
            Phone          = req.Phone.Trim(),
            MembershipDate = req.MembershipDate == default ? DateTime.Today : req.MembershipDate
        });

        return Created($"/api/members/{newId}",
            ApiResponse<object>.Ok(new { id = newId }, "Member registered."));
    }

    // ── PUT /api/members/5 ───────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMemberRequest req)
    {
        // Validate status value
        var validStatuses = new[] { "Active", "Suspended", "Expired" };
        if (!validStatuses.Contains(req.Status))
            return BadRequest(ApiResponse<Member>.Fail("Status must be Active, Suspended, or Expired."));

        using var conn = _db.CreateConnection();

        var rows = await conn.ExecuteAsync(
            @"UPDATE members
              SET first_name = @FirstName, last_name = @LastName,
                  phone = @Phone, membership_date = @MembershipDate,
                  status = @Status
              WHERE id = @Id",
            new
            {
                Id             = id,
                FirstName      = req.FirstName.Trim(),
                LastName       = req.LastName.Trim(),
                Phone          = req.Phone.Trim(),
                MembershipDate = req.MembershipDate,
                Status         = req.Status
            });

        if (rows == 0)
            return NotFound(ApiResponse<Member>.Fail($"Member {id} not found."));

        return Ok(ApiResponse<Member>.Ok(null, "Member updated."));
    }
}
