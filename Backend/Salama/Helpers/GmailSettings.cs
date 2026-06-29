namespace Salama.Helpers
{
    public class SmtpSettings
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; } = 587;
        public string SenderEmail { get; set; } = null!;
        public string SenderPassword { get; set; } = null!;
        public string SenderName { get; set; } = "Salama";
    }
}
