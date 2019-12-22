using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.Templates.ServiceBus.Queue
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddCommandLine(args);
                    configuration.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    //services.AddServiceBusQueueMessagePump<OrdersMessagePump>(configuration => configuration["ARCUS_SERVICEBUS_CONNECTIONSTRING"]);
                    services.AddTcpHealthProbes();
                });
    }

    //public class Order
    //{
    //    [JsonProperty]
    //    public string Id { get; set; }

    //    [JsonProperty]
    //    public int Amount { get; set; }

    //    [JsonProperty]
    //    public string ArticleNumber { get; set; }
    //}

    //public class OrderCreatedEventData
    //{
    //    public OrderCreatedEventData(string id, int amount, string articleNumber, MessageCorrelationInfo correlationInfo)
    //    {
    //        Id = id;
    //        Amount = amount;
    //        ArticleNumber = articleNumber;
    //        CorrelationInfo = correlationInfo;
    //    }

    //    public string Id { get; set; }
    //    public int Amount { get; set; }
    //    public string ArticleNumber { get; set; }
    //    public MessageCorrelationInfo CorrelationInfo { get; set; }
    //}

    //public class OrderCreatedEvent : EventGridEvent<OrderCreatedEventData>
    //{
    //    private const string DefaultDataVersion = "1";
    //    private const string DefaultEventType = "Arcus.Samples.Orders.OrderCreated";

    //    public OrderCreatedEvent(string eventId, string orderId, int amount, string articleNumber, MessageCorrelationInfo correlationInfo)
    //        : base(eventId, subject: "order-created",
    //               new OrderCreatedEventData(orderId, amount, articleNumber, correlationInfo), 
    //               DefaultDataVersion,
    //               DefaultEventType)
    //    {
    //    }

    //    [JsonConstructor]
    //    private OrderCreatedEvent()
    //    {
    //    }
    //}

    //public class OrdersMessagePump : AzureServiceBusMessagePump<Order>
    //{
    //    private readonly IEventGridPublisher _eventGridPublisher;

    //    /// <summary>
    //    /// Constructor
    //    /// </summary>
    //    /// <param name="configuration">Configuration of the application</param>
    //    /// <param name="serviceProvider">Collection of services that are configured</param>
    //    /// <param name="logger">Logger to write telemetry to</param>
    //    public OrdersMessagePump(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<OrdersMessagePump> logger)
    //        : base(configuration, serviceProvider, logger)
    //    {
    //        var eventGridTopic = configuration.GetValue<string>("EVENTGRID_TOPIC_URI");
    //        var eventGridKey = configuration.GetValue<string>("EVENTGRID_AUTH_KEY");

    //        _eventGridPublisher = 
    //            EventGridPublisherBuilder
    //                .ForTopic(eventGridTopic)
    //                .UsingAuthenticationKey(eventGridKey)
    //                .Build();
    //    }

    //    /// <inheritdoc />
    //    protected override async Task ProcessMessageAsync(
    //        Order orderMessage, 
    //        AzureServiceBusMessageContext messageContext, 
    //        MessageCorrelationInfo correlationInfo, 
    //        CancellationToken cancellationToken)
    //    {
    //        Logger.LogInformation(
    //            "Processing order {OrderId} for {OrderAmount} units of {OrderArticle}", 
    //            orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber);

    //        await PublishEventToEventGridAsync(orderMessage, correlationInfo.OperationId, correlationInfo);

    //        Logger.LogInformation("Order {OrderId} processed", orderMessage.Id);
    //    }

    //    private async Task PublishEventToEventGridAsync(Order orderMessage, string operationId, MessageCorrelationInfo correlationInfo)
    //    {
    //        var orderCreatedEvent = new OrderCreatedEvent(operationId, orderMessage.Id, orderMessage.Amount, orderMessage.ArticleNumber, correlationInfo);
            
    //        await _eventGridPublisher.PublishAsync(orderCreatedEvent);

    //        Logger.LogInformation("Event {EventId} was published with subject {EventSubject}", orderCreatedEvent.Id, orderCreatedEvent.Subject);
    //    }
    //}
}
