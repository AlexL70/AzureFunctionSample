using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace My.Functions
{
    public class OrderAddress
    {
		public string? Address { get; set; }
		public string? City { get; set; }
		public string? State { get; set; }
	}

    public class ProductItem {
        public int ProductId { get; set; }
        public decimal Cost { get; set; }
    }

    public class OrderRequest {
        public int OrderNumber { get; set; }
	    public DateTime OrderDate { get; set; }
	    public string? CustomerName { get; set; }
	    public int CustomerId { get; set; }
        public OrderAddress? ShipTo { get; set; }
        public IEnumerable<ProductItem>? Items { get; set; }
    }

    public class OrderResponse
    {
        public string? Message { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal OrderTotal { get; set; }
    }

    public class ProcessOrder
    {
        private readonly ILogger _logger;

        public ProcessOrder(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessOrder>();
        }

        [Function("ProcessOrder")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            _logger.LogInformation("C# HTTP Order Processing function started.");
            var responseBody = new OrderResponse();

            if (req.Body == null || !req.Body.CanRead || req.Body.Length == 0)
            {
                responseBody.Message = "Request must have a body.";
                return createResonpse(req, HttpStatusCode.InternalServerError, JsonSerializer.Serialize(responseBody, serializeOptions));
            }
            OrderRequest order;
            try {
                var reader = new StreamReader(req.Body);
                var bodyStr = reader.ReadToEnd();
                order = JsonSerializer.Deserialize<OrderRequest>(bodyStr, serializeOptions);
            } catch (Exception e)
            {
                responseBody.Message = $"Error reading request body: {e.Message}";
                return createResonpse(req, HttpStatusCode.BadRequest, JsonSerializer.Serialize(responseBody, serializeOptions));
            }

            var orderSubTotal = order?.Items?.Sum(_ => _.Cost) ?? 0;
            responseBody.DiscountValue = orderSubTotal > 500 ? orderSubTotal * 0.10M : 0M;
            responseBody.Message = "OK";
            responseBody.OrderTotal = orderSubTotal - responseBody.DiscountValue;
            _logger.LogInformation("C# HTTP Order Processing function finished processing the request.");

            return createResonpse(req, HttpStatusCode.OK, JsonSerializer.Serialize(responseBody, serializeOptions));
        }

        private HttpResponseData createResonpse(HttpRequestData req, HttpStatusCode status, string body)
        {
            var response = req.CreateResponse(status);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(body);
            return response;
        }
    }
}
