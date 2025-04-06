using System.Text.Json.Serialization;

namespace UserCreateMailSaga.Events
{
    public class UserCreationFailedEvent : DomainEvent
    {
        [JsonInclude]
        public string Email { get; private set; }

        [JsonInclude]
        public string FailureReason { get; private set; }

        [JsonInclude]
        public DateTime FailureTime { get; private set; }

        [JsonConstructor]
        public UserCreationFailedEvent()
        {
            Email = string.Empty;
            FailureReason = string.Empty;
            FailureTime = DateTime.UtcNow;
        }

        public UserCreationFailedEvent(string email, string failureReason, DateTime failureTime)
        {
            Email = email;
            FailureReason = failureReason;
            FailureTime = failureTime;
        }
    }
} 