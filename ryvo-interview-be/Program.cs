using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ryvo_interview_be.Api;
using ryvo_interview_be.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Ryvo")
    ?? "Data Source=ryvo.db";

builder.Services.AddDbContext<RyvoDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<TimesheetStore>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ryvo Timesheets API",
        Version = "v1",
        Description =
            "Contractor timesheets and client invoices, persisted in SQLite. " +
            "Call POST /api/dev/login/{userId} with an id from GET /api/dev/users, " +
            "or click Authorize and paste a user id to send X-User-Id on every call."
    });

    options.AddSecurityDefinition("DevUser", new OpenApiSecurityScheme
    {
        Name = "X-User-Id",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "A user id from GET /api/dev/users (Acme = 1111..., Budi = 2222...)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("DevUser", document), new List<string>() }
    });
});

builder.Services.AddCors(options =>
    options.AddPolicy("dev", policy =>
        policy
            .WithOrigins("http://localhost:3000", "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

var app = builder.Build();

DbInitializer.Initialize(app);

app.UseExceptionHandler();
app.UseCors("dev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ryvo API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapRyvoApi();

app.Run();
