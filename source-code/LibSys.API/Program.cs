// ============================================================
//  Program.cs
//  ──────────
//  What is this?
//  This is the entry point of the entire C# application.
//  It runs first when you start the API.
//
//  It does 3 things:
//  1. REGISTER SERVICES  → tell the app what classes exist
//                          and how to inject them
//  2. CONFIGURE PIPELINE → set up middleware (CORS, Swagger, etc.)
//  3. RUN               → start listening for HTTP requests
//
//  DEPENDENCY INJECTION (DI) explained simply:
//  Instead of controllers creating their own DB connections,
//  they "ask" for one through their constructor parameter.
//  The DI system automatically provides the right object.
//  This makes code easier to test and maintain.
//
//  CORS explained:
//  By default browsers block requests from one origin
//  (your HTML files on file:// or localhost:3000) to another
//  (your API on localhost:5000). CORS tells the browser
//  "it's OK, I allow this." We allow everything for local dev.
// ============================================================

using LibSys.API.Data;

var builder = WebApplication.CreateBuilder(args);

// ── 1. REGISTER SERVICES ────────────────────────────────────

// Register our database context as a Singleton
// (one instance shared across the app's lifetime)
builder.Services.AddSingleton<LibSysDbContext>();

// Register controllers — scans all *Controller.cs files
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Makes JSON property names camelCase automatically
        // C#: FineAmount → JSON: fineAmount
        // This matches what JavaScript expects
        opts.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Register Swagger — API documentation + testing UI
// Visit http://localhost:5000/swagger when running
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "LibSys API",
        Version     = "v1",
        Description = "Library Management System REST API — EDP Activity"
    });
});

// Register CORS policy — allows your HTML frontend to call this API
// "AllowAll" is fine for local development. In production, restrict
// this to your actual domain.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()   // any URL can call this API
            .AllowAnyMethod()   // GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader();  // any HTTP headers
    });
});

var app = builder.Build();

// ── 2. CONFIGURE MIDDLEWARE PIPELINE ────────────────────────
// Middleware = code that runs on every request, in order.

// Show Swagger UI in development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibSys API v1");
        c.RoutePrefix = "swagger"; // http://localhost:5000/swagger
    });
}

// Apply the CORS policy — MUST come before UseAuthorization
app.UseCors("AllowAll");

// Route requests to the right controller
app.UseAuthorization();
app.MapControllers();

// ── 3. RUN ──────────────────────────────────────────────────
// Starts the web server. The API will listen at:
//   http://localhost:5000   (HTTP)
//   https://localhost:5001  (HTTPS — may need cert setup)
//
// HOW TO START:
//   dotnet run
// Or in Visual Studio: press F5 / the Run button

app.Run();
