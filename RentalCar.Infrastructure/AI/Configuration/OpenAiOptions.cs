namespace RentalCar.AI.Configuration
{
    public class OpenAiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "openai/gpt-oss-20b";
        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 2;
        public string PromptVersion { get; set; } = "assistant-v2";
    }
}
