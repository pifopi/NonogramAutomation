namespace NonogramAutomation
{
    public static class SettingsManager
    {
        public static GlobalSettings GlobalSettings { get; set; } = new();

        public static T LoadSettings<T>(string settingsFile)
        {
            if (!System.IO.File.Exists(settingsFile))
            {
                throw new System.IO.FileNotFoundException(settingsFile);
            }
            string jsonString = System.IO.File.ReadAllText(settingsFile);
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString) ?? throw new Exception($"{settingsFile} cannot be read properly. Verify you fill it properly");
        }

        public static string GetDiscordBotToken()
        {
            string key = "DISCORD_BOT_TOKEN_NonogramAutomation";
            return Environment.GetEnvironmentVariable(key) ?? throw new Exception($"{key} is not set");
        }
    }
}
