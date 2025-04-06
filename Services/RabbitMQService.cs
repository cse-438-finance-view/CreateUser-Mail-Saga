using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UserCreateMailSaga.Configuration;
using UserCreateMailSaga.Events;
using RabbitMQ.Client.Events;
using UserCreateMailSaga.Commands;

namespace UserCreateMailSaga.Services
{
    public class RabbitMQService : IDisposable
    {
        private readonly RabbitMQConfig _config;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(
            IOptions<RabbitMQConfig> config,
            IServiceProvider serviceProvider,
            ILogger<RabbitMQService> logger)
        {
            _config = config.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(
                    exchange: _config.UserCreatedExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _channel.ExchangeDeclare(
                    exchange: _config.EmailCommandExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                _channel.QueueDeclare(
                    queue: _config.UserCreatedQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                _channel.QueueDeclare(
                    queue: _config.UserCreationFailedQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                _channel.QueueDeclare(
                    queue: _config.EmailCommandQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                _channel.QueueBind(
                    queue: _config.UserCreatedQueue,
                    exchange: _config.UserCreatedExchange,
                    routingKey: _config.UserCreatedRoutingKey);

                _channel.QueueBind(
                    queue: _config.UserCreationFailedQueue,
                    exchange: _config.UserCreationFailedExchange,
                    routingKey: _config.UserCreationFailedRoutingKey);

                _channel.QueueBind(
                    queue: _config.EmailCommandQueue,
                    exchange: _config.EmailCommandExchange,
                    routingKey: _config.EmailCommandRoutingKey);

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish RabbitMQ connection");
                throw;
            }
        }

        public void StartConsumingUserEvents(Func<UserCreatedEvent, Task> userCreatedHandler, Func<UserCreationFailedEvent, Task> userCreationFailedHandler)
        {
            ConsumeUserCreatedEvents(userCreatedHandler);
            ConsumeUserCreationFailedEvents(userCreationFailedHandler);
        }

        private void ConsumeUserCreatedEvents(Func<UserCreatedEvent, Task> handler)
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var @event = JsonSerializer.Deserialize<UserCreatedEvent>(message);

                    if (@event != null)
                    {
                        _logger.LogInformation("UserCreatedEvent received: {UserId}, {Email}", @event.UserId, @event.Email);
                        await handler(@event);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UserCreatedEvent");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: _config.UserCreatedQueue,
                autoAck: false,
                consumer: consumer);
        }

        private void ConsumeUserCreationFailedEvents(Func<UserCreationFailedEvent, Task> handler)
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var @event = JsonSerializer.Deserialize<UserCreationFailedEvent>(message);

                    if (@event != null)
                    {
                        _logger.LogInformation("UserCreationFailedEvent received: {Email}, {FailureReason}", @event.Email, @event.FailureReason);
                        await handler(@event);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UserCreationFailedEvent");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: _config.UserCreationFailedQueue,
                autoAck: false,
                consumer: consumer);
        }

        public void PublishEmailCommand(EmailCommand command)
        {
            try
            {
                var message = JsonSerializer.Serialize(command);
                var body = Encoding.UTF8.GetBytes(message);

                _channel.BasicPublish(
                    exchange: _config.EmailCommandExchange,
                    routingKey: _config.EmailCommandRoutingKey,
                    basicProperties: null,
                    body: body);

                _logger.LogInformation("Published EmailCommand for {Email} with type {MailType}", 
                    command.Email, command.MailType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing EmailCommand");
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
} 