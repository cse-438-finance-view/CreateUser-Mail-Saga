namespace UserCreateMailSaga.Configuration
{
    public class RabbitMQConfig
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        
        public string UserCreatedExchange { get; set; } = "user.events";
        public string UserCreatedQueue { get; set; } = "user.created.queue";
        public string UserCreatedRoutingKey { get; set; } = "user.created";
        
        public string UserCreationFailedExchange { get; set; } = "user.events";
        public string UserCreationFailedQueue { get; set; } = "user.creation.failed.queue";
        public string UserCreationFailedRoutingKey { get; set; } = "user.creation.failed";
        
        public string EmailCommandExchange { get; set; } = "saga.commands";
        public string EmailCommandQueue { get; set; } = "email.command.queue";
        public string EmailCommandRoutingKey { get; set; } = "saga.email.command";
    }
} 