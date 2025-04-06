namespace UserCreateMailSaga.Sagas
{
    public enum SagaStatus
    {
        NotStarted,
        UserCreated,
        UserCreationFailed,
        EmailSent,
        EmailFailed,
        Completed,
        Failed
    }

    public class SagaState
    {
        public Guid SagaId { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public SagaStatus Status { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public SagaState()
        {
            SagaId = Guid.NewGuid();
            UserId = string.Empty;
            Email = string.Empty;
            Status = SagaStatus.NotStarted;
            CreatedAt = DateTime.UtcNow;
        }
    }
} 