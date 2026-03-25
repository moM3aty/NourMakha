using System.Text;
using System.Text.Json;
using PerfumeStore.Models;

namespace PerfumeStore.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(Order order, string returnUrlBase);
        Task<(bool IsPaid, string OrderNumber)> VerifyPaymentAsync(string transactionId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        // مفاتيح الجيل الجديد لسلطنة عمان


        // الرابط الأساسي لسيرفرات سلطنة عمان
        private readonly string _baseUrl = "https://oman.paymob.com";

        public PaymentService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> CreateCheckoutSessionAsync(Order order, string returnUrlBase)
        {
            var client = _httpClientFactory.CreateClient();

            // تحويل المبلغ إلى بيسات (1 ريال = 1000 بيسة)
            int amountBaisas = (int)Math.Round(order.GrandTotal * 1000);

            // تحديد الـ Integration ID بناءً على اختيار العميل (Apay, Omannet, International)
            int integrationId = 48305; // الافتراضي International
            if (order.PaymentMethod.StartsWith("Paymob_"))
            {
                int.TryParse(order.PaymentMethod.Split('_')[1], out integrationId);
            }

            // بناء الطلب حسب نظام Paymob الحديث (Intention API)
            var payload = new
            {
                amount = amountBaisas,
                currency = "OMR",
                payment_methods = new[] { integrationId },
                special_reference = order.OrderNumber,
                billing_data = new
                {
                    first_name = string.IsNullOrEmpty(order.ShippingFirstName) ? "Customer" : order.ShippingFirstName,
                    last_name = string.IsNullOrEmpty(order.ShippingLastName) ? "Name" : order.ShippingLastName,
                    email = string.IsNullOrEmpty(order.ShippingEmail) ? "info@nourmakha.com" : order.ShippingEmail,
                    phone_number = string.IsNullOrEmpty(order.ShippingPhone) ? "+9680000000" : order.ShippingPhone,
                    apartment = "NA",
                    floor = "NA",
                    street = string.IsNullOrEmpty(order.ShippingAddress) ? "NA" : order.ShippingAddress,
                    building = "NA",
                    city = string.IsNullOrEmpty(order.ShippingCity) ? "NA" : order.ShippingCity,
                    country = "OM",
                    state = "NA"
                }
            };

            // الاتصال المباشر بسيرفر عمان عبر الـ Secret Key
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Token {_secretKey}");

            var response = await client.PostAsync($"{_baseUrl}/v1/intention/",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Paymob Oman API Error: {responseContent}");
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            var clientSecret = jsonDoc.RootElement.GetProperty("client_secret").GetString();

            // توجيه العميل إلى صفحة الدفع الموحدة الآمنة على سيرفر عمان
            return $"{_baseUrl}/unifiedcheckout/?publicKey={_publicKey}&clientSecret={clientSecret}";
        }

        public async Task<(bool IsPaid, string OrderNumber)> VerifyPaymentAsync(string transactionId)
        {
            var client = _httpClientFactory.CreateClient();

            // الاستعلام عن حالة الطلب من Paymob Oman
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Token {_secretKey}");

            var transResponse = await client.GetAsync($"{_baseUrl}/api/acceptance/transactions/{transactionId}");

            if (transResponse.IsSuccessStatusCode)
            {
                var transData = JsonDocument.Parse(await transResponse.Content.ReadAsStringAsync());
                var root = transData.RootElement;

                bool isSuccess = root.GetProperty("success").GetBoolean();
                string orderNumber = root.GetProperty("order").GetProperty("merchant_order_id").GetString() ?? "";

                return (isSuccess, orderNumber);
            }

            return (false, string.Empty);
        }
    }
}