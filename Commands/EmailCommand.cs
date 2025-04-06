using System.Text.Json.Serialization;

namespace UserCreateMailSaga.Commands
{
    public class EmailCommand
    {
        public string Email { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string MailType { get; set; } 
        public string? FailureReason { get; set; }

        public EmailCommand()
        {
            Email = string.Empty;
            MailType = string.Empty;
        }
    }
} 