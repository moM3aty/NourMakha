using System.Security.Cryptography;
using System.Text;

namespace PerfumeStore.Services
{
    public interface IPaymentService
    {
        Dictionary<string, string> PreparePaymentRequest(string orderId, decimal amount, string lang);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;

        public PaymentService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Dictionary<string, string> PreparePaymentRequest(string orderId, decimal amount, string lang)
        {
            var settings = _configuration.GetSection("AmwalPaySettings");
            string formattedAmount = amount.ToString("F3");

            var parameters = new Dictionary<string, string>
            {
                { "action", settings["Action"] ?? "1" },
                { "merchant_id", settings["MerchantId"] },
                { "terminal_id", settings["TerminalId"] },
                { "amount", formattedAmount },
                { "currency", "OMR" },
                { "trackid", orderId },
                { "response_url", settings["ResponseUrl"] },
                { "udf1", "PerfumeStore" },
                { "udf2", "" }, { "udf3", "" }, { "udf4", "" }, { "udf5", "" }
            };

            // Hashing Logic (Concatenation then Hash)
            var rawString = new StringBuilder();
            rawString.Append(settings["SecureHashKey"]);
            rawString.Append("|");
            rawString.Append(parameters["merchant_id"]);
            rawString.Append("|");
            rawString.Append(parameters["terminal_id"]);
            rawString.Append("|");
            rawString.Append(parameters["trackid"]);
            rawString.Append("|");
            rawString.Append(parameters["amount"]);
            rawString.Append("|");
            rawString.Append(parameters["currency"]);
            rawString.Append("|");
            rawString.Append(parameters["action"]);

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(rawString.ToString());
                var hash = sha256.ComputeHash(bytes);
                parameters.Add("request_hash", BitConverter.ToString(hash).Replace("-", "").ToUpper());
            }

            return parameters;
        }
    }
}