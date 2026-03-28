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

     
        // الرابط الأساسي لسيرفرات سلطنة عمان (مهم جداً)
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

            // تحديد رقم بوابة الدفع (Integration ID) بناءً على اختيار العميل
            int integrationId = 48305; // الافتراضي International (Visa/Mastercard)
            if (order.PaymentMethod.StartsWith("Paymob_"))
            {
                int.TryParse(order.PaymentMethod.Split('_')[1], out integrationId);
            }

            // تنظيف رقم الهاتف (بوابات الدفع ترفض الأرقام الوهمية أو التي تحتوي حروف)
            var cleanPhone = new string((order.ShippingPhone ?? "98185589").Where(char.IsDigit).ToArray());
            if (cleanPhone.Length < 8) cleanPhone = "98185589";

            // ==========================================
            // الخطوة 1: إنشاء نية دفع (Intention) - خطوة واحدة تغني عن 3 خطوات!
            // ==========================================
            var payload = new
            {
                amount = amountBaisas,
                currency = "OMR",
                payment_methods = new[] { integrationId },
                special_reference = order.OrderNumber,
                billing_data = new
                {
                    first_name = string.IsNullOrWhiteSpace(order.ShippingFirstName) ? "Nour" : order.ShippingFirstName,
                    last_name = string.IsNullOrWhiteSpace(order.ShippingLastName) ? "Customer" : order.ShippingLastName,
                    email = string.IsNullOrWhiteSpace(order.ShippingEmail) ? "info@nourmakha.com" : order.ShippingEmail,
                    phone_number = "+968" + cleanPhone, // إضافة كود الدولة لتجنب الرفض
                    apartment = "NA",
                    floor = "NA",
                    street = string.IsNullOrWhiteSpace(order.ShippingAddress) ? "Muscat" : order.ShippingAddress,
                    building = "NA",
                    city = string.IsNullOrWhiteSpace(order.ShippingCity) ? "Muscat" : order.ShippingCity,
                    country = "OM",
                    state = "NA"
                }
            };

            // إرسال الطلب المباشر باستخدام الـ Secret Key
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Token {_secretKey}");

            var response = await client.PostAsync($"{_baseUrl}/v1/intention/",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Paymob Intention API Error: {responseContent}");
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            var clientSecret = jsonDoc.RootElement.GetProperty("client_secret").GetString();

            // ==========================================
            // الخطوة 2: التوجيه الذكي لشاشة الدفع (بدون IFrame)
            // ==========================================
            // استخدام رابط الـ Unified Checkout الأنيق
            return $"{_baseUrl}/unifiedcheckout/?publicKey={_publicKey}&clientSecret={clientSecret}";
        }

        public async Task<(bool IsPaid, string OrderNumber)> VerifyPaymentAsync(string transactionId)
        {
            var client = _httpClientFactory.CreateClient();

            // 1. الحصول على توكن المصادقة للتأكد من المعاملة
            var authPayload = new { api_key = _apiKey };
            var authResponse = await client.PostAsync($"{_baseUrl}/api/auth/tokens",
                new StringContent(JsonSerializer.Serialize(authPayload), Encoding.UTF8, "application/json"));

            if (!authResponse.IsSuccessStatusCode) return (false, string.Empty);

            var authData = JsonDocument.Parse(await authResponse.Content.ReadAsStringAsync());
            var authToken = authData.RootElement.GetProperty("token").GetString();

            // 2. الاستعلام من سيرفر Paymob للتأكد أن العميل دفع بالفعل
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
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
