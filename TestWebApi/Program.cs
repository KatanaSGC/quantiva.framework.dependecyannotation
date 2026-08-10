using quantiva.framework.dependecyannotation;
using TestWebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Register all annotated services via DependencyAnnotation
builder.Services.AddDependencyAnnotation();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Test endpoint using the annotated GreetingService
app.MapGet("/greet/{name}", (string name, IGreetingService greetingService) =>
{
    return Results.Ok(new { message = greetingService.Greet(name) });
})
.WithName("GetGreeting");

app.Run();
