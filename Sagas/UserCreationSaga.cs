using UserCreateMailSaga.Events;
using UserCreateMailSaga.Services;
using UserCreateMailSaga.Commands;

namespace UserCreateMailSaga.Sagas
{
    public class UserCreationSaga
    {
        private readonly ILogger<UserCreationSaga> _logger;
        private readonly Dictionary<string, SagaState> _sagaStates;
        private readonly RabbitMQService _rabbitMQService;

        public UserCreationSaga(ILogger<UserCreationSaga> logger, RabbitMQService rabbitMQService)
        {
            _logger = logger;
            _rabbitMQService = rabbitMQService;
            _sagaStates = new Dictionary<string, SagaState>();
        }

        public async Task HandleUserCreatedEvent(UserCreatedEvent @event)
        {
            _logger.LogInformation("Processing UserCreatedEvent for email: {Email}", @event.Email);

            var sagaState = new SagaState
            {
                UserId = @event.UserId,
                Email = @event.Email,
                Name = @event.Name,
                Surname = @event.Surname,
                Status = SagaStatus.UserCreated,
                CreatedAt = @event.CreatedAt
            };

            _sagaStates[@event.Email] = sagaState;

            await SendWelcomeEmail(sagaState);
        }

        public Task HandleUserCreationFailedEvent(UserCreationFailedEvent @event)
        {
            _logger.LogInformation("Processing UserCreationFailedEvent for email: {Email}", @event.Email);

            var sagaState = new SagaState
            {
                Email = @event.Email,
                Status = SagaStatus.UserCreationFailed,
                FailureReason = @event.FailureReason,
                CreatedAt = @event.CreatedAt
            };

            _sagaStates[@event.Email] = sagaState;

            try
            {
                _logger.LogInformation("Sending failure notification email command for {Email}", @event.Email);
                
                var emailCommand = new EmailCommand
                {
                    Email = @event.Email,
                    MailType = "Failure",
                    FailureReason = @event.FailureReason
                };

                _rabbitMQService.PublishEmailCommand(emailCommand);
                
                _logger.LogInformation("Failure notification email command sent for {Email}", @event.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send failure notification email command for {Email}", @event.Email);
            }

            return Task.CompletedTask;
        }

        private async Task SendWelcomeEmail(SagaState sagaState)
        {
            try
            {
                _logger.LogInformation("Sending welcome email command for {Email}", sagaState.Email);
                
                var emailCommand = new EmailCommand
                {
                    Email = sagaState.Email,
                    Name = sagaState.Name,
                    Surname = sagaState.Surname,
                    MailType = "Welcome"
                };

                _rabbitMQService.PublishEmailCommand(emailCommand);
                
                sagaState.Status = SagaStatus.EmailSent;
                sagaState.CompletedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Welcome email command sent successfully for {Email}", sagaState.Email);
                
                CompleteSaga(sagaState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email command for {Email}", sagaState.Email);
                sagaState.Status = SagaStatus.EmailFailed;
                sagaState.FailureReason = $"Failed to send welcome email command: {ex.Message}";
                
                FailSaga(sagaState);
            }
        }

        private void CompleteSaga(SagaState sagaState)
        {
            sagaState.Status = SagaStatus.Completed;
            sagaState.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("Saga completed successfully for user {UserId}, email {Email}", 
                sagaState.UserId, sagaState.Email);
        }

        private void FailSaga(SagaState sagaState)
        {
            sagaState.Status = SagaStatus.Failed;
            sagaState.CompletedAt = DateTime.UtcNow;
            _logger.LogWarning("Saga failed for email {Email} with reason: {Reason}", 
                sagaState.Email, sagaState.FailureReason);
            
            
        }
    }
} 