using Prahelika.Calculators.Api.Endpoints;
using Prahelika.Calculators.Core.Basic;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

builder.Services.AddOpenApi();                                          // builds the API description
builder.Services.AddSingleton<IBasicCalculator, BasicCalculator>();     // dependency injection

var app = builder.Build();

app.MapOpenApi();                                                       // serves /openapi/v1.json
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Prahelika.Calculators API v1");
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapBasicEndpoints();
app.MapScientificEndpoints();

app.Run();