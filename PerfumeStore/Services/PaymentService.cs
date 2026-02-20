using System.Text;
using System.Text.Json;
using PerfumeStore.Models;

namespace PerfumeStore.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(Order order, string returnUrlBase);
        Task<(bool IsPaid, string OrderNumber)> VerifyPaymentAsync(string sessionId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> CreateCheckoutSessionAsync(Order order, string returnUrlBase)
        {
            var settings = _configuration.GetSection("ThawaniSettings");
            var secretKey = settings["SecretKey"];
            var pubKey = settings["PublishableKey"];
            var baseUrl = settings["BaseUrl"] ?? "https://checkout.thawani.om/api/v1";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("thawani-api-key", secretKey);

            // نظام ثواني يطلب المبلغ بالوحدة الأساسية (بيسة) 1 OMR = 1000 Baisa
            long amountInBaisa = (long)Math.Round(order.GrandTotal * 1000);

            var payload = new
            {
                client_reference_id = order.OrderNumber,
                mode = "payment",
                products = new[]
                {
                    new
                    {
                        name = $"Order #{order.OrderNumber} - NourMakha",
                        quantity = 1,
                        unit_amount = amountInBaisa
                    }
                },
                success_url = $"{returnUrlBase}/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}",
                cancel_url = $"{returnUrlBase}/Payment/Cancel",
                metadata = new
                {
                    order_id = order.Id.ToString(),
                    customer_name = order.CustomerName
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/checkout/session", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseString);
                var sessionId = doc.RootElement.GetProperty("data").GetProperty("session_id").GetString();

                // استخراج الرابط الأساسي لصفحة الدفع بناءً على البيئة (Test or Prod)
                string checkoutDomain = baseUrl.Replace("/api/v1", "");

                // إعادة رابط صفحة الدفع الخاص بثواني لتوجيه العميل إليه
                return $"{checkoutDomain}/pay/{sessionId}?key={pubKey}";
            }

            throw new Exception("Thawani API Error: " + responseString);
        }

        public async Task<(bool IsPaid, string OrderNumber)> VerifyPaymentAsync(string sessionId)
        {
            var settings = _configuration.GetSection("ThawaniSettings");
            var secretKey = settings["SecretKey"];
            var baseUrl = settings["BaseUrl"] ?? "https://checkout.thawani.om/api/v1";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("thawani-api-key", secretKey);

            var response = await client.GetAsync($"{baseUrl}/checkout/session/{sessionId}");
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var data = doc.RootElement.GetProperty("data");

                var status = data.GetProperty("payment_status").GetString();
                var orderNumber = data.GetProperty("client_reference_id").GetString();

                return (status == "paid", orderNumber ?? "");
            }

            return (false, string.Empty);
        }
    }
}