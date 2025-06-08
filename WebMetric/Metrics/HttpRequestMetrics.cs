using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace WebMetric.Metrics
{
    public class HttpRequestMetrics
    {
        private readonly Counter<long> _httpRequestsCounter;

        public HttpRequestMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("WebMetric.Metrics.HttpRequestMetrics", "1.0.0");

            _httpRequestsCounter = meter.CreateCounter<long>("http.requests");
        }

        public void RecordRequest(HttpContext context)
        {
            var method = context.Request.Method;
            var path = context.Request.Path.ToString();
            var statusCode = context.Response.StatusCode;

            // Registra a contagem da requisição com tags para agregação
            _httpRequestsCounter.Add(1,
                new KeyValuePair<string, object?>("http.method", method),
                new KeyValuePair<string, object?>("http.path", path),
                new KeyValuePair<string, object?>("http.status_code", statusCode));
        }
    }
}
