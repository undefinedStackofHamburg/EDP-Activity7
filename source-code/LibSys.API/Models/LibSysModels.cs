// ============================================================
//  Models/LibSysModels.cs
//  ──────────────────────
//  What is this?
//  These are C# "model" classes — they represent the shape
//  of your data. Each class maps 1-to-1 with a database table.
//
//  Dapper reads a SQL query result and automatically fills
//  these objects with the right values — like a translation
//  layer between your database rows and C# code.
//
//  Example flow:
//    SQL:  SELECT id, name, description FROM categories
//    →  Dapper fills a List<Category> for you automatically
// ============================================================

namespace LibSys.API.Models;

// ── CATEGORY ──────────────────────────────────────────────────
// Mirrors the "categories" table
public class Category
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
}

// ── AUTHOR ────────────────────────────────────────────────────
// Mirrors the "authors" table
public class Author
{
    public int    Id          { get; set; }
    public string FirstName   { get; set; } = "";
    public string LastName    { get; set; } = "";
    public string Nationality { get; set; } = "";
    public int    BirthYear   { get; set; }

    // Computed helper — not stored in DB, just convenient in C#
    public string FullName => $"{FirstName} {LastName}";
}

// ── BOOK ──────────────────────────────────────────────────────
// Mirrors the "books" table
public class Book
{
    public int    Id         { get; set; }
    public string Title      { get; set; } = "";
    public int    AuthorId   { get; set; }
    public int    CatId      { get; set; }
    public string Isbn       { get; set; } = "";
    public int    YearPub    { get; set; }
    public int    Total      { get; set; }
    public int    Available  { get; set; }

    // These come from JOIN queries — not columns in "books"
    public string AuthorName   { get; set; } = "";
    public string CategoryName { get; set; } = "";
}

// ── MEMBER ────────────────────────────────────────────────────
// Mirrors the "members" table
public class Member
{
    public int       Id             { get; set; }
    public string    FirstName      { get; set; } = "";
    public string    LastName       { get; set; } = "";
    public string    Phone          { get; set; } = "";
    public DateTime  MembershipDate { get; set; }
    public string    Status         { get; set; } = "Active";

    public string FullName => $"{FirstName} {LastName}";
}

// ── LOAN ──────────────────────────────────────────────────────
// Mirrors the "loans" table
public class Loan
{
    public int       Id         { get; set; }
    public int       BookId     { get; set; }
    public int       MemberId   { get; set; }
    public DateTime  LoanDate   { get; set; }
    public DateTime  DueDate    { get; set; }
    public DateTime? ReturnDate { get; set; }  // nullable — null if not returned yet
    public decimal   FineAmount { get; set; }
    public string    Status     { get; set; } = "Active";

    // Joined fields from related tables
    public string MemberName { get; set; } = "";
    public string BookTitle  { get; set; } = "";
}

// ── USER ACCOUNT ───────────────────────────────────────────────
// Mirrors the "users" table.
// NOTE: password_hash is NEVER included in this class —
//       we never want to accidentally send it to the browser.
public class UserAccount
{
    public int      Id        { get; set; }
    public string   Username  { get; set; } = "";
    public string   FullName  { get; set; } = "";
    public string   Email     { get; set; } = "";
    public string   Role      { get; set; } = "Staff";
    public string   Status    { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

// ── REQUEST BODIES ────────────────────────────────────────────
// These are used when the frontend sends data TO the API.
// They're separate from the main models so we only accept
// what we actually need (not the whole object with IDs, etc.)

public class CreateLoanRequest
{
    public int BookId   { get; set; }
    public int MemberId { get; set; }
    public int Days     { get; set; } = 14;  // default loan duration
}

public class ReturnBookRequest
{
    public int      LoanId     { get; set; }
    public DateTime ReturnDate { get; set; }
}

public class CreateBookRequest
{
    public string Title     { get; set; } = "";
    public int    AuthorId  { get; set; }
    public int    CatId     { get; set; }
    public string Isbn      { get; set; } = "";
    public int    YearPub   { get; set; }
    public int    Total     { get; set; } = 1;
}

public class UpdateBookRequest : CreateBookRequest
{
    public int Available { get; set; }
}

public class CreateMemberRequest
{
    public string FirstName      { get; set; } = "";
    public string LastName       { get; set; } = "";
    public string Phone          { get; set; } = "";
    public DateTime MembershipDate { get; set; } = DateTime.Today;
}

public class UpdateMemberRequest : CreateMemberRequest
{
    public string Status { get; set; } = "Active";
}

public class CreateAuthorRequest
{
    public string FirstName   { get; set; } = "";
    public string LastName    { get; set; } = "";
    public string Nationality { get; set; } = "";
    public int    BirthYear   { get; set; }
}

public class CreateCategoryRequest
{
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
}

// ── USER REQUEST BODIES ───────────────────────────────────────

public class CreateUserRequest
{
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email    { get; set; } = "";
    public string Role     { get; set; } = "Staff";
    public string Password { get; set; } = "";
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = "";
    public string Email    { get; set; } = "";
    public string Role     { get; set; } = "Staff";
    public string Status   { get; set; } = "Active";
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword     { get; set; } = "";
}

// ── API RESPONSE WRAPPER ──────────────────────────────────────
// Every API response uses this wrapper so the frontend always
// gets a consistent shape:
// { success: true, data: {...}, message: "..." }
public class ApiResponse<T>
{
    public bool   Success { get; set; } = true;
    public string Message { get; set; } = "";
    public T?     Data    { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}
