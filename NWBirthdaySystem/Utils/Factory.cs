using NWBirthdaySystem.Library;
using Serilog;
using Serilog.Formatting.Json;

namespace NWBirthdaySystem.Utils
{
    internal class Factory
    {
        public static ILogger GetLogger()
        {
            LoggerConfiguration configuration = new LoggerConfiguration()
                .WriteTo.File(new JsonFormatter(renderMessage: true), "logs.json", fileSizeLimitBytes: 16777216)
                .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug);
            return configuration.CreateLogger();
        }

        public static IDatabaseContext GetDatabaseContext(ILogger logger, string connectionString)
        {
            return new PostGres(logger, connectionString);
        }

        public static IMessageContext GetMessageContext(ILogger logger, HttpClient httpClient)
        {
            return new Telegram(logger, httpClient);
        }
    }
}
