using System.Text.Json.Serialization;
using Prahelika.Calculators.Api.Endpoints;
using Prahelika.Calculators.Core.Basic;

var builder = WebApplication.CreateBuilder(args);

// Describe numbers as plain numbers in the API description, so Swagger UI's form accepts them.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

// Lets errors be written in the standard problem-details format.
builder.Services.AddProblemDetails();

// By default, bad input in development throws an exception. Return a normal 400 instead,
// so development and production behave the same.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

builder.Services.AddOpenApi();                                          // builds the API description
builder.Services.AddSingleton<IBasicCalculator, BasicCalculator>();     // dependency injection

var app = builder.Build();

app.UseExceptionHandler();      // an unexpected crash becomes a 500 problem response, not a stack trace
app.UseStatusCodePages();       // empty error responses (400, 404, 405) become problem responses

app.MapOpenApi();                                                       // serves /openapi/v1.json
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Prahelika.Calculators API v1");
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapBasicEndpoints();
app.MapScientificEndpoints();

app.Run();