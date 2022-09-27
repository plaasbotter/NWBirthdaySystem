using Newtonsoft.Json;
using NWBirthdaySystem.Models;
using NWBirthdaySystem.Utils;
using Serilog;

namespace NWBirthdaySystem.Library
{
    internal class Telegram : IMessageContext
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;

        public Telegram(ILogger logger, HttpClient client)
        {
            _logger = logger;
            _client = client;
        }

        public bool SendMessage(string text, string chatId)
        {
            string urlString = string.Empty;
            bool success = false;
            try
            {
                urlString = string.Format(@"https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}", Config.configVaraibles.TelegramAPIToken, chatId, text);
                using (HttpResponseMessage response = _client.GetAsync(urlString).GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();
                }
                success = true;
            }
            catch (Exception err)
            {
                _logger.Error(err, "[{0}] [{1}]", "TelegramAPI.SendMessage", urlString);
            }
            return success;
        }

        public MessageBase ReadMessages()
        {
            TelegramMessage returnValue = new TelegramMessage();
            string rawResponse = "";
            try
            {
                var urlString = string.Format(@"https://api.telegram.org/bot{0}/getUpdates", Config.configVaraibles.TelegramAPIToken);
                using (HttpResponseMessage response = _client.GetAsync(urlString).GetAwaiter().GetResult())
                {
                    rawResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                returnValue = JsonConvert.DeserializeObject<TelegramMessage>(rawResponse);
            }
            catch (Exception err)
            {
                _logger.Error(err, "[{0}]", "TelegramAPI.ScanForMessages");
            }
            return returnValue;
        }
    }
}
