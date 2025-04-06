using System.Text.Json.Serialization;

namespace UserCreateMailSaga.Events
{
    public abstract class DomainEvent
    {
        [JsonInclude]
        public Guid Id { get; private set; }

        [JsonInclude]
        public DateTime CreatedAt { get; private set; }

        protected DomainEvent()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
} 