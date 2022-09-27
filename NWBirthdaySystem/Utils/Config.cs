using Newtonsoft.Json;
using System.Text;

namespace NWBirthdaySystem.Utils
{
    public static class Config
    {
        public static ConfigVaraibles configVaraibles = new ConfigVaraibles();

        public static void LoadConfig()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (FileStream fs = new FileStream("config.nw", FileMode.OpenOrCreate, FileAccess.Read))
                {
                    fs.CopyTo(ms);
                }
                ConfigVaraibles? tempConfigVariables = JsonConvert.DeserializeObject<ConfigVaraibles>(Encoding.UTF8.GetString(ms.ToArray()));
                if (tempConfigVariables != null)
                {
                    configVaraibles = tempConfigVariables;
                }
            }
        }

        public static void SaveConfig()
        {
            using (FileStream fs = new FileStream("config.nw", FileMode.Create, FileAccess.Write))
            {
                fs.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(configVaraibles)));
            }
        }

        public static string GetConnectionString()
        {
            return $"Server={configVaraibles.ip};Username={configVaraibles.username};Database={configVaraibles.database};Port={configVaraibles.port};Password={configVaraibles.password};SSLMode=Prefer";
        }
    }

    public class ConfigVaraibles
    {
        public int sleepTimer;
        public string TelegramAPIToken = "";
        public string ip = "";
        public string username = "";
        public string database = "";
        public string port = "";
        public string password = "";
    }
}

