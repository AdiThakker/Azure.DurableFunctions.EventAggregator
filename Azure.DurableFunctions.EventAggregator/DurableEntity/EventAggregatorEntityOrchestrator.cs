using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.DurableFunctions.EventAggregator.DurableEntity
{
    public  class EventAggregatorEntityOrchestrator
    {
        public ILogger<EventAggregatorEntityOrchestrator> Logger { get; }

        public EventAggregatorEntityOrchestrator(ILogger<EventAggregatorEntityOrchestrator> logger) => Logger = logger;

        //[FunctionName("Event-EntitySubscriber")]
        //public async Task ReceiveEventAsync([EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableClient client)
        //{
        //    Logger.LogInformation($"Received Event: {eventGridEvent.Data.ToString()}");
        //    await UpdateEventEntityAsync(eventGridEvent, client);
        //}

        //[FunctionName("Dependencies-EntitySubscriber")]
        //public async Task DependenciesSubscriber([EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableClient client)
        //{
        //    Logger.LogInformation($"Received Dependency: {eventGridEvent.Data.ToString()}");
        //    await UpdateEventEntityAsync(eventGridEvent, client);
        //}

        private async Task UpdateEventEntityAsync(EventGridEvent eventGridEvent, IDurableClient client)
        {
            await client.SignalEntityAsync<IEventAggregatorEntity>(eventGridEvent.Subject, _ => _.EventReceivedAsync(eventGridEvent));
        }
    }
}
