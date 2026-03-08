using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// In tests we use an in-memory SQLite provider, so avoid registering the SQL Server
// provider when running in the 'Testing' environment to prevent multiple provider
// registrations. The test host will replace the DbContext registration.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<PatientAccessDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "PatientAccessDb")));
}

builder.Services.AddControllers();

// Register Appointment Service
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<CoverageService>();

builder.Services.AddProblemDetails();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

// Add Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // https://localhost:####/swagger
    //app.MapOpenApi();
}

app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["traceId"] = context.TraceIdentifier,
        ["path"] = context.Request.Path.Value ?? "",
        ["method"] = context.Request.Method,
    }))
    {
        await next();
    }
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
