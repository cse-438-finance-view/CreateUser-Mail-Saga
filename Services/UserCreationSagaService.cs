using Microsoft.Extensions.Options;
using UserCreateMailSaga.Configuration;
using UserCreateMailSaga.Sagas;

namespace UserCreateMailSaga.Services
{
    public class UserCreationSagaService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserCreationSagaService> _logger;
        private readonly RabbitMQConfig _rabbitMQConfig;

        public UserCreationSagaService(
            IServiceProvider serviceProvider,
            IOptions<RabbitMQConfig> rabbitMQConfig,
            ILogger<UserCreationSagaService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _rabbitMQConfig = rabbitMQConfig.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("User Creation Saga Service is starting");

            await Task.Yield();

            using var scope = _serviceProvider.CreateScope();
            var rabbitMQService = scope.ServiceProvider.GetRequiredService<RabbitMQService>();
            var userCreationSaga = scope.ServiceProvider.GetRequiredService<UserCreationSaga>();

            rabbitMQService.StartConsumingUserEvents(
                userCreatedHandler: async @event => await userCreationSaga.HandleUserCreatedEvent(@event),
                userCreationFailedHandler: async @event => await userCreationSaga.HandleUserCreationFailedEvent(@event)
            );

            _logger.LogInformation("User Creation Saga Service started and listening for events");

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("User Creation Saga Service is stopping");
            }
        }
    }
} 