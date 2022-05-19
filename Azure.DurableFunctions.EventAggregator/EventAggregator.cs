// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=Function1

namespace Azure.DurableFunctions.EventAggregator
{
    public class EventAggregator
    {
        public ILogger<EventAggregator> Logger { get; }

        public EventAggregator(ILogger<EventAggregator> logger) => Logger = logger;
        
        [FunctionName("Events-Subscriber")]
        public void EventsSubscriber([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            Logger.LogInformation(eventGridEvent.Data.ToString());
        }

        [FunctionName("Dependencies-Subscriber")]
        public void DependenciesSubscriber([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            Logger.LogInformation(eventGridEvent.Data.ToString());
        }

        [FunctionName("Event-Aggregator")]
        public async Task AggregateEventsAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            
        }
    }
}
