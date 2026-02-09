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
            return Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") ?? throw new Exception("DISCORD_BOT_TOKEN is not set");
        }
    }
}
