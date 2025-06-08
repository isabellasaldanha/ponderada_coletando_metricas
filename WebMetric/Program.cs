using OpenTelemetry.Metrics;
using WebMetric.Metrics;
using WebMetric.Models;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configuração OpenTelemetry com PrometheusExporter e métricas padrão do ASP.NET Core
builder.Services.AddOpenTelemetry()
    .WithMetrics(metricsBuilder =>
    {
        metricsBuilder.AddPrometheusExporter();

        metricsBuilder.AddMeter("Microsoft.AspNetCore.Hosting");
        metricsBuilder.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
        metricsBuilder.AddMeter("Contoso.Web"); 
        metricsBuilder.AddMeter("WebMetric.HttpRequestMetrics"); 

        metricsBuilder.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[]
                {
                    0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1,
                    2.5, 5, 7.5, 10
                }
            });
    });

builder.Services.AddSingleton<HttpRequestMetrics>();
builder.Services.AddSingleton<ContosoMetrics>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var tagsFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMetricsTagsFeature>();

    if (tagsFeature != null)
    {
        var utmMedium = context.Request.Query["utm_medium"].ToString();

        var source = utmMedium switch
        {
            "" => "none",
            "social" => "social",
            "email" => "email",
            "organic" => "organic",
            _ => "other"
        };

        tagsFeature.Tags.Add(new KeyValuePair<string, object?>("mkt_medium", source));
    }

    await next();
});

app.Use(async (context, next) =>
{
    var httpRequestMetrics = app.Services.GetRequiredService<HttpRequestMetrics>();

    await next();
    httpRequestMetrics.RecordRequest(context);
});

// Endpoint para simular uma venda e registrar métrica customizada
app.MapPost("/complete-sale", (SaleModel model, ContosoMetrics metrics) =>
{
    metrics.ProductSold(model.ProductName, model.QuantitySold);
    return Results.Ok($"Venda registrada: {model.ProductName}, Quantidade: {model.QuantitySold}");
});

app.MapPrometheusScrapingEndpoint();

app.MapGet("/", () => "Hello OpenTelemetry! ticks:" + DateTime.Now.Ticks.ToString()[^3..]);

app.Run();