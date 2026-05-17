// ============================================================
//  Controllers/ReportsController.cs
// ============================================================

using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Dapper;
using LibSys.API.Data;
using LibSys.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibSys.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly LibSysDbContext _db;

    private static readonly XLColor AccentOrange = XLColor.FromHtml("#E8621A");
    private static readonly XLColor LightBorder  = XLColor.FromHtml("#F0D9C4");

    // Logo embedded as base64 — no separate file needed at runtime.
    private static readonly byte[] LogoBytes = Convert.FromBase64String(
        "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"
    );

    public ReportsController(LibSysDbContext db) => _db = db;

    private class MemberCount
    {
        public int MemberId { get; set; }
        public int Total    { get; set; }
    }

    // ─────────────────────────────────────────────────────────
    //  HEADER / FOOTER HELPERS
    // ─────────────────────────────────────────────────────────

    // Rows 1-4: logo + system info block.
    // Column headers go on row 5, data from row 6 onward.
    //
    // IMPORTANT: the MemoryStream for the logo must NOT be disposed before
    // wb.SaveAs() - ClosedXML holds a reference to it internally.
    // We return it so the caller can keep it alive in a using block that
    // wraps the whole workbook lifetime.
    private static MemoryStream AddLogoHeader(IXLWorksheet ws, int colCount, string reportTitle)
    {
        // Do NOT wrap in using here - caller owns the lifetime.
        var logoStream = new MemoryStream(LogoBytes);

        ws.AddPicture(logoStream, XLPictureFormat.Png)
          .MoveTo(ws.Cell(1, 1))
          .WithSize(52, 52);

        ws.Row(1).Height = 14;
        ws.Row(2).Height = 20;
        ws.Row(3).Height = 13;
        ws.Row(4).Height = 13;

        // Column A is reserved for the logo image; text goes in B onward.
        ws.Cell(1, 2).Value = "LibSys";
        ws.Cell(1, 2).Style.Font.Bold      = true;
        ws.Cell(1, 2).Style.Font.FontSize  = 14;
        ws.Cell(1, 2).Style.Font.FontColor = AccentOrange;

        ws.Cell(2, 2).Value = reportTitle;
        ws.Cell(2, 2).Style.Font.Bold     = true;
        ws.Cell(2, 2).Style.Font.FontSize = 12;

        ws.Cell(3, 2).Value = $"Generated: {DateTime.Now:MMMM dd, yyyy  hh:mm tt}";
        ws.Cell(3, 2).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(3, 2).Style.Font.FontSize  = 8;

        ws.Cell(4, 2).Value = "Library Management System  |  EDP Activity";
        ws.Cell(4, 2).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(4, 2).Style.Font.FontSize  = 8;

        // Span text cells across all columns
        ws.Range(1, 2, 1, colCount).Merge();
        ws.Range(2, 2, 2, colCount).Merge();
        ws.Range(3, 2, 3, colCount).Merge();
        ws.Range(4, 2, 4, colCount).Merge();

        // Bottom border separating header from column labels
        ws.Range(4, 1, 4, colCount).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Range(4, 1, 4, colCount).Style.Border.BottomBorderColor = LightBorder;

        return logoStream;
    }

    private static void StyleColumnHeaders(IXLRow headerRow)
    {
        foreach (var cell in headerRow.Cells())
        {
            cell.Style.Font.Bold            = true;
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Fill.BackgroundColor = AccentOrange;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder  = XLBorderStyleValues.Medium;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#C04010");
        }
    }

    private static void StyleDataRow(IXLRow row, int rowIndex, int colCount)
    {
        var bg = rowIndex % 2 == 0 ? XLColor.FromHtml("#FDF7F2") : XLColor.White;
        for (int c = 1; c <= colCount; c++)
        {
            row.Cell(c).Style.Fill.BackgroundColor = bg;
            row.Cell(c).Style.Border.BottomBorder  = XLBorderStyleValues.Thin;
            row.Cell(c).Style.Border.BottomBorderColor = LightBorder;
        }
    }

    // Signature block appended after the last data row.
    private static void AddSignatureBlock(IXLWorksheet ws, int afterRow, int colCount, string signatory)
    {
        int r = afterRow + 2;

        ws.Cell(r, 1).Value = "Prepared by:";
        ws.Cell(r, 1).Style.Font.Bold     = true;
        ws.Cell(r, 1).Style.Font.FontSize = 9;
        ws.Range(r, 1, r, 3).Merge();

        r += 3; // blank space for physical signature
        ws.Range(r, 1, r, 3).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 1, r, 3).Style.Border.BottomBorderColor = XLColor.Black;

        r += 1;
        ws.Cell(r, 1).Value = string.IsNullOrWhiteSpace(signatory)
            ? "________________________"
            : signatory;
        ws.Cell(r, 1).Style.Font.Bold     = true;
        ws.Cell(r, 1).Style.Font.FontSize = 10;
        ws.Range(r, 1, r, 3).Merge();

        r += 1;
        ws.Cell(r, 1).Value = "Authorized Signatory";
        ws.Cell(r, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(r, 1).Style.Font.FontSize  = 8;
        ws.Range(r, 1, r, 3).Merge();

        r += 2;
        ws.Cell(r, 1).Value = "This is a system-generated report from LibSys Library Management System.";
        ws.Cell(r, 1).Style.Font.Italic    = true;
        ws.Cell(r, 1).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(r, 1).Style.Font.FontSize  = 7;
        ws.Range(r, 1, r, colCount).Merge();
    }

    private static void FinalizeSheet(IXLWorksheet ws, int colCount)
    {
        ws.Column(1).Width = 7;
        ws.Columns(2, colCount).AdjustToContents();
        ws.SheetView.FreezeRows(5);
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
    }

    // Saves workbook to bytes. logoStreams must stay open until this returns.
    private static FileContentResult ExcelFile(XLWorkbook wb, string filename,
        params MemoryStream[] logoStreams)
    {
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            wb.SaveAs(stream);
            bytes = stream.ToArray();
        }
        // Dispose logo streams now that SaveAs is done
        foreach (var s in logoStreams) s.Dispose();

        return new FileContentResult(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = filename
        };
    }

    // Chart data table on Sheet 2
    private static void WriteChartData(IXLWorksheet ws, string[] headers, object[][] rows)
    {
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = headers[c];
            ws.Cell(1, c + 1).Style.Font.Bold = true;
            ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F0D9C4");
        }
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
            {
                var cell = ws.Cell(r + 2, c + 1);
                if      (rows[r][c] is decimal d) cell.Value = (double)d;
                else if (rows[r][c] is int i)     cell.Value = i;
                else                               cell.Value = rows[r][c]?.ToString() ?? "";
            }
        ws.Columns().AdjustToContents();
    }

    private static void AddChartLabel(IXLWorksheet ws, string title)
    {
        int labelCol = ws.LastColumnUsed()?.ColumnNumber() + 2 ?? 4;
        ws.Cell(1, labelCol).Value = title;
        ws.Cell(1, labelCol).Style.Font.Bold     = true;
        ws.Cell(1, labelCol).Style.Font.FontSize = 12;
        ws.Cell(2, labelCol).Value = "Select the data table, then use Insert > Chart in Excel to visualize.";
        ws.Cell(2, labelCol).Style.Font.FontColor = XLColor.Gray;
        ws.Cell(2, labelCol).Style.Font.FontSize  = 9;
        ws.Column(labelCol).Width = 55;
    }

    // ── GET /api/reports/loans ──────────────────────────────
    [HttpGet("loans")]
    public async Task<IActionResult> LoansReport([FromQuery] string? signatory = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            var loans = (await conn.QueryAsync<Loan>(@"
                SELECT l.id, l.loan_date AS LoanDate, l.due_date AS DueDate,
                       l.return_date AS ReturnDate, l.fine_amount AS FineAmount, l.status,
                       CONCAT(m.first_name, ' ', m.last_name) AS MemberName,
                       b.title AS BookTitle
                FROM loans l
                JOIN members m ON m.id = l.member_id
                JOIN books   b ON b.id = l.book_id
                ORDER BY l.id")).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Loans Summary");
            var logo = AddLogoHeader(ws, 8, "Loans Summary Report");

            var hdrs = new[] { "#", "Member", "Book", "Loan Date", "Due Date", "Return Date", "Status", "Fine (P)" };
            for (int i = 0; i < hdrs.Length; i++) ws.Cell(5, i + 1).Value = hdrs[i];
            StyleColumnHeaders(ws.Row(5));

            int row = 6;
            foreach (var l in loans)
            {
                ws.Cell(row, 1).Value = l.Id;
                ws.Cell(row, 2).Value = l.MemberName;
                ws.Cell(row, 3).Value = l.BookTitle;
                ws.Cell(row, 4).Value = l.LoanDate.ToString("MMM dd, yyyy");
                ws.Cell(row, 5).Value = l.DueDate.ToString("MMM dd, yyyy");
                ws.Cell(row, 6).Value = l.ReturnDate.HasValue ? l.ReturnDate.Value.ToString("MMM dd, yyyy") : "-";
                ws.Cell(row, 7).Value = l.Status;
                ws.Cell(row, 8).Value = l.FineAmount;
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 7).Style.Font.FontColor = l.Status switch {
                    "Active"   => XLColor.FromHtml("#4A7DC0"),
                    "Returned" => AccentOrange,
                    "Overdue"  => XLColor.FromHtml("#C0392B"),
                    _          => XLColor.Gray
                };
                ws.Cell(row, 7).Style.Font.Bold = true;
                StyleDataRow(ws.Row(row), row, 8);
                row++;
            }
            ws.Cell(row + 1, 7).Value = "Total Fines:";
            ws.Cell(row + 1, 7).Style.Font.Bold = true;
            ws.Cell(row + 1, 8).Value = loans.Sum(l => l.FineAmount);
            ws.Cell(row + 1, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row + 1, 8).Style.Font.Bold      = true;
            ws.Cell(row + 1, 8).Style.Font.FontColor = AccentOrange;

            AddSignatureBlock(ws, row + 1, 8, signatory ?? "");
            FinalizeSheet(ws, 8);

            var wc = wb.Worksheets.Add("Chart Data");
            WriteChartData(wc, new[] { "Status", "Count" }, new object[][] {
                new object[] { "Active",   loans.Count(l => l.Status == "Active")   },
                new object[] { "Returned", loans.Count(l => l.Status == "Returned") },
                new object[] { "Overdue",  loans.Count(l => l.Status == "Overdue")  },
            });
            AddChartLabel(wc, "Chart: Loans by Status");

            return ExcelFile(wb, $"LibSys_Loans_{DateTime.Now:yyyyMMdd}.xlsx", logo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Failed to generate Loans report: {ex.Message}" });
        }
    }

    // ── GET /api/reports/overdue ────────────────────────────
    [HttpGet("overdue")]
    public async Task<IActionResult> OverdueReport([FromQuery] string? signatory = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            var loans = (await conn.QueryAsync<Loan>(@"
                SELECT l.id, l.due_date AS DueDate, l.fine_amount AS FineAmount, l.status,
                       CONCAT(m.first_name, ' ', m.last_name) AS MemberName,
                       b.title AS BookTitle
                FROM loans l
                JOIN members m ON m.id = l.member_id
                JOIN books   b ON b.id = l.book_id
                WHERE l.status = 'Overdue'
                ORDER BY l.due_date")).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Overdue Report");
            var logo = AddLogoHeader(ws, 6, "Overdue Loans Report");

            var hdrs = new[] { "#", "Member", "Book", "Due Date", "Days Late", "Fine (P)" };
            for (int i = 0; i < hdrs.Length; i++) ws.Cell(5, i + 1).Value = hdrs[i];
            StyleColumnHeaders(ws.Row(5));

            int row = 6;
            foreach (var l in loans)
            {
                var daysLate = (DateTime.Today - l.DueDate.Date).Days;
                ws.Cell(row, 1).Value = l.Id;
                ws.Cell(row, 2).Value = l.MemberName;
                ws.Cell(row, 3).Value = l.BookTitle;
                ws.Cell(row, 4).Value = l.DueDate.ToString("MMM dd, yyyy");
                ws.Cell(row, 4).Style.Font.FontColor = XLColor.FromHtml("#C0392B");
                ws.Cell(row, 5).Value = $"{daysLate} days";
                ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#C0392B");
                ws.Cell(row, 5).Style.Font.Bold      = true;
                ws.Cell(row, 6).Value = l.FineAmount;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                StyleDataRow(ws.Row(row), row, 6);
                row++;
            }
            AddSignatureBlock(ws, row, 6, signatory ?? "");
            FinalizeSheet(ws, 6);

            var wc = wb.Worksheets.Add("Chart Data");
            var buckets = new[] {
                ("1-7 days",   loans.Count(l => (DateTime.Today - l.DueDate.Date).Days is >= 1  and <= 7)),
                ("8-14 days",  loans.Count(l => (DateTime.Today - l.DueDate.Date).Days is >= 8  and <= 14)),
                ("15-30 days", loans.Count(l => (DateTime.Today - l.DueDate.Date).Days is >= 15 and <= 30)),
                (">30 days",   loans.Count(l => (DateTime.Today - l.DueDate.Date).Days > 30)),
            };
            WriteChartData(wc, new[] { "Days Late", "Books" },
                buckets.Select(b => new object[] { b.Item1, b.Item2 }).ToArray());
            AddChartLabel(wc, "Chart: Overdue by Days Late");

            return ExcelFile(wb, $"LibSys_Overdue_{DateTime.Now:yyyyMMdd}.xlsx", logo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Failed to generate Overdue report: {ex.Message}" });
        }
    }

    // ── GET /api/reports/inventory ──────────────────────────
    [HttpGet("inventory")]
    public async Task<IActionResult> InventoryReport([FromQuery] string? signatory = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            var books = (await conn.QueryAsync<Book>(@"
                SELECT b.id, b.title, b.isbn, b.year_pub AS YearPub,
                       b.total, b.available,
                       CONCAT(a.first_name, ' ', a.last_name) AS AuthorName,
                       c.name AS CategoryName
                FROM books b
                JOIN authors    a ON a.id = b.author_id
                JOIN categories c ON c.id = b.cat_id
                ORDER BY c.name, b.title")).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Books Inventory");
            var logo = AddLogoHeader(ws, 8, "Books Inventory Report");

            var hdrs = new[] { "#", "Title", "Author", "Category", "ISBN", "Year", "Total", "Available" };
            for (int i = 0; i < hdrs.Length; i++) ws.Cell(5, i + 1).Value = hdrs[i];
            StyleColumnHeaders(ws.Row(5));

            int row = 6;
            foreach (var b in books)
            {
                ws.Cell(row, 1).Value = b.Id;
                ws.Cell(row, 2).Value = b.Title;
                ws.Cell(row, 3).Value = b.AuthorName;
                ws.Cell(row, 4).Value = b.CategoryName;
                ws.Cell(row, 5).Value = b.Isbn;
                ws.Cell(row, 6).Value = b.YearPub;
                ws.Cell(row, 7).Value = b.Total;
                ws.Cell(row, 8).Value = b.Available;
                ws.Cell(row, 8).Style.Font.FontColor = b.Available == 0
                    ? XLColor.FromHtml("#C0392B")
                    : b.Available < b.Total * 0.3
                        ? XLColor.FromHtml("#D97706")
                        : XLColor.FromHtml("#4A8C3A");
                ws.Cell(row, 8).Style.Font.Bold = true;
                StyleDataRow(ws.Row(row), row, 8);
                row++;
            }
            ws.Cell(row + 1, 6).Value = "Totals:";
            ws.Cell(row + 1, 6).Style.Font.Bold = true;
            ws.Cell(row + 1, 7).Value = books.Sum(b => b.Total);
            ws.Cell(row + 1, 7).Style.Font.Bold = true;
            ws.Cell(row + 1, 8).Value = books.Sum(b => b.Available);
            ws.Cell(row + 1, 8).Style.Font.Bold      = true;
            ws.Cell(row + 1, 8).Style.Font.FontColor = XLColor.FromHtml("#4A8C3A");

            AddSignatureBlock(ws, row + 1, 8, signatory ?? "");
            FinalizeSheet(ws, 8);

            var wc = wb.Worksheets.Add("Chart Data");
            var byCategory = books.GroupBy(b => b.CategoryName)
                .Select(g => new object[] {
                    g.Key,
                    g.Sum(b => b.Available),
                    g.Sum(b => b.Total) - g.Sum(b => b.Available)
                }).ToArray();
            WriteChartData(wc, new[] { "Category", "Available", "On Loan" }, byCategory);
            AddChartLabel(wc, "Chart: Stock by Category");

            return ExcelFile(wb, $"LibSys_Inventory_{DateTime.Now:yyyyMMdd}.xlsx", logo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Failed to generate Inventory report: {ex.Message}" });
        }
    }

    // ── GET /api/reports/fines ──────────────────────────────
    [HttpGet("fines")]
    public async Task<IActionResult> FinesReport([FromQuery] string? signatory = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            var loans = (await conn.QueryAsync<Loan>(@"
                SELECT l.id, l.due_date AS DueDate, l.return_date AS ReturnDate,
                       l.fine_amount AS FineAmount, l.status,
                       CONCAT(m.first_name, ' ', m.last_name) AS MemberName,
                       b.title AS BookTitle
                FROM loans l
                JOIN members m ON m.id = l.member_id
                JOIN books   b ON b.id = l.book_id
                WHERE l.fine_amount > 0
                ORDER BY l.fine_amount DESC")).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Fines Report");
            var logo = AddLogoHeader(ws, 7, "Fines Collection Report");

            var hdrs = new[] { "Loan #", "Member", "Book", "Due Date", "Return Date", "Status", "Fine (P)" };
            for (int i = 0; i < hdrs.Length; i++) ws.Cell(5, i + 1).Value = hdrs[i];
            StyleColumnHeaders(ws.Row(5));

            int row = 6;
            foreach (var l in loans)
            {
                ws.Cell(row, 1).Value = $"#{l.Id}";
                ws.Cell(row, 2).Value = l.MemberName;
                ws.Cell(row, 3).Value = l.BookTitle;
                ws.Cell(row, 4).Value = l.DueDate.ToString("MMM dd, yyyy");
                ws.Cell(row, 5).Value = l.ReturnDate.HasValue ? l.ReturnDate.Value.ToString("MMM dd, yyyy") : "-";
                ws.Cell(row, 6).Value = l.Status;
                ws.Cell(row, 7).Value = l.FineAmount;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 7).Style.Font.FontColor      = XLColor.FromHtml("#D97706");
                ws.Cell(row, 7).Style.Font.Bold           = true;
                StyleDataRow(ws.Row(row), row, 7);
                row++;
            }
            var total = loans.Sum(l => l.FineAmount);
            ws.Cell(row + 1, 6).Value = "Grand Total:";
            ws.Cell(row + 1, 6).Style.Font.Bold = true;
            ws.Cell(row + 1, 7).Value = total;
            ws.Cell(row + 1, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row + 1, 7).Style.Font.Bold      = true;
            ws.Cell(row + 1, 7).Style.Font.FontSize  = 12;
            ws.Cell(row + 1, 7).Style.Font.FontColor = AccentOrange;

            AddSignatureBlock(ws, row + 1, 7, signatory ?? "");
            FinalizeSheet(ws, 7);

            var wc = wb.Worksheets.Add("Chart Data");
            decimal collected = loans.Where(l => l.Status == "Returned").Sum(l => l.FineAmount);
            decimal pending   = loans.Where(l => l.Status != "Returned").Sum(l => l.FineAmount);
            WriteChartData(wc, new[] { "Category", "Amount (P)" }, new object[][] {
                new object[] { "Collected", collected },
                new object[] { "Pending",   pending   },
            });
            AddChartLabel(wc, "Chart: Fines Collected vs Pending");

            return ExcelFile(wb, $"LibSys_Fines_{DateTime.Now:yyyyMMdd}.xlsx", logo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Failed to generate Fines report: {ex.Message}" });
        }
    }

    // ── GET /api/reports/members ────────────────────────────
    [HttpGet("members")]
    public async Task<IActionResult> MembersReport([FromQuery] string? signatory = null)
    {
        try
        {
            using var conn = _db.CreateConnection();
            var members = (await conn.QueryAsync<Member>(@"
                SELECT id, first_name AS FirstName, last_name AS LastName,
                       phone, membership_date AS MembershipDate, status
                FROM members
                ORDER BY status, last_name")).ToList();

            var loanCounts = (await conn.QueryAsync<MemberCount>(
                "SELECT member_id AS MemberId, COUNT(*) AS Total FROM loans GROUP BY member_id"))
                .ToDictionary(r => r.MemberId, r => r.Total);

            var activeLoans = (await conn.QueryAsync<MemberCount>(
                "SELECT member_id AS MemberId, COUNT(*) AS Total FROM loans WHERE status='Active' GROUP BY member_id"))
                .ToDictionary(r => r.MemberId, r => r.Total);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Members Report");
            var logo = AddLogoHeader(ws, 7, "Members Report");

            var hdrs = new[] { "#", "Full Name", "Phone", "Member Since", "Status", "Total Loans", "Active Loans" };
            for (int i = 0; i < hdrs.Length; i++) ws.Cell(5, i + 1).Value = hdrs[i];
            StyleColumnHeaders(ws.Row(5));

            int row = 6;
            foreach (var m in members)
            {
                loanCounts.TryGetValue(m.Id, out int total);
                activeLoans.TryGetValue(m.Id, out int active);
                ws.Cell(row, 1).Value = m.Id;
                ws.Cell(row, 2).Value = m.FullName;
                ws.Cell(row, 3).Value = m.Phone;
                ws.Cell(row, 4).Value = m.MembershipDate.ToString("MMM dd, yyyy");
                ws.Cell(row, 5).Value = m.Status;
                ws.Cell(row, 5).Style.Font.FontColor = m.Status switch {
                    "Active"    => XLColor.FromHtml("#4A8C3A"),
                    "Suspended" => XLColor.FromHtml("#D97706"),
                    "Expired"   => XLColor.Gray,
                    _           => XLColor.Black
                };
                ws.Cell(row, 5).Style.Font.Bold = true;
                ws.Cell(row, 6).Value = total;
                ws.Cell(row, 7).Value = active;
                if (active > 0)
                {
                    ws.Cell(row, 7).Style.Font.FontColor = XLColor.FromHtml("#4A7DC0");
                    ws.Cell(row, 7).Style.Font.Bold      = true;
                }
                StyleDataRow(ws.Row(row), row, 7);
                row++;
            }
            AddSignatureBlock(ws, row, 7, signatory ?? "");
            FinalizeSheet(ws, 7);

            var wc = wb.Worksheets.Add("Chart Data");
            WriteChartData(wc, new[] { "Status", "Count" }, new object[][] {
                new object[] { "Active",    members.Count(m => m.Status == "Active")    },
                new object[] { "Suspended", members.Count(m => m.Status == "Suspended") },
                new object[] { "Expired",   members.Count(m => m.Status == "Expired")   },
            });
            AddChartLabel(wc, "Chart: Members by Status");

            return ExcelFile(wb, $"LibSys_Members_{DateTime.Now:yyyyMMdd}.xlsx", logo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Failed to generate Members report: {ex.Message}" });
        }
    }
}
