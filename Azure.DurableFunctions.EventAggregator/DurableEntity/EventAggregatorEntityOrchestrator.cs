//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Azure.DurableFunctions.EventAggregator.DurableEntity
//{
//    public class EventAggregatorEntityOrchestrator
//    {
//        private static ConcurrentDictionary<string, List<string>> dependencies;

//        static EventAggregatorEntityOrchestrator()
//        {
//            dependencies = new ConcurrentDictionary<string, List<string>>();
//            dependencies.TryAdd("Event A", new List<string>() { "A-1", "A-2" });
//            dependencies.TryAdd("Event B", new List<string>() { "B-1" });
//            dependencies.TryAdd("Event C", new List<string>() { "" });
//        }

//        public ILogger<EventAggregatorEntityOrchestrator> Logger { get; }

//        public EventAggregatorEntityOrchestrator(ILogger<EventAggregatorEntityOrchestrator> logger) => Logger = logger;

//        [FunctionName("Event-EntitySubscriber")]
//        public async Task ReceiveEventAsync([EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableClient client)
//        {
//            Logger.LogInformation($"Received Event: {eventGridEvent.Data.ToString()}");
//            // Check if any dependencies     
//            dependencies.TryGetValue(eventGridEvent.Subject.ToString(), out List<string> dependenciesList);
//            //await client.SignalEntityAsync<IEventAggregatorEntity>(eventGridEvent.Subject, _ => _.EventReceivedAsync(eventGridEvent, dependenciesList));
//            await UpdateEventEntityAsync(eventGridEvent, client);
//        }

//        [FunctionName("Dependencies-EntitySubscriber")]
//        public async Task DependenciesSubscriber([EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableClient client)
//        {
//            Logger.LogInformation($"Received Dependency: {eventGridEvent.Data.ToString()}");
//            await UpdateEventEntityAsync(eventGridEvent, client);
//        }

//        private async Task UpdateEventEntityAsync(EventGridEvent eventGridEvent, IDurableClient client)
//        {
//            var entity = await client.ReadEntityStateAsync<IEventAggregatorEntity>(new EntityId(eventGridEvent.Subject, eventGridEvent.Subject));
//            if (!entity.EntityExists)
//            {
//                // Check if any dependencies     
//                if (dependencies.TryGetValue(eventGridEvent.Subject.ToString(), out List<string> dependenciesList))
//                {

//                }

                
//            }
            
//        }
//    }
//}
