// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName=Function1

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Azure.DurableFunctions.EventAggregator.DurableFunction
{
    public class EventAggregatorOrchestrator
    {
        private static ConcurrentDictionary<string, List<string>> dependencies;

        static EventAggregatorOrchestrator()
        {
            dependencies = new ConcurrentDictionary<string, List<string>>();
            dependencies.TryAdd("Event A", new List<string>() { "A-1", "A-2" });
            dependencies.TryAdd("Event B", new List<string>() { "B-1" });
            dependencies.TryAdd("Event C", new List<string>() { "" });
        }

        public ILogger<EventAggregatorOrchestrator> Logger { get; }

        public EventAggregatorOrchestrator(ILogger<EventAggregatorOrchestrator> logger) => Logger = logger;

        [FunctionName("Event-Subscriber")]
        public async Task ReceiveEventAsync([EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableClient client)
        {
            Logger.LogInformation($"Received Event: {eventGridEvent.Data.ToString()}");
            await ReceiveOrUpdateEventsAsync(eventGridEvent, client);
        }

        [FunctionName("Dependencies-Subscriber")]
        public async Task DependenciesSubscriber([EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableClient client)
        {
            Logger.LogInformation($"Received Dependency: {eventGridEvent.Data.ToString()}");
            await ReceiveOrUpdateEventsAsync(eventGridEvent, client);
        }

        [FunctionName("Event-Aggregator-Orchestrator")]
        public async Task AggregateEventsAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            // Get the list of dependencies to aggregate for a given event
            var receivedEvent = context.GetInput<EventGridEvent>();

            // Check if any dependencies     
            if (dependencies.TryGetValue(receivedEvent.Data.ToString(), out List<string> dependenciesList))
            {
                if (dependenciesList.Any())
                {
                    var endTime = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(120));// Durable Timer 
                    using var cts = new CancellationTokenSource();
                    var timeout = context.CreateTimer<List<string>>(endTime, default, cts.Token);
                    var remainingDepdencies = DependenciesReceivedAsync(context, dependenciesList, cts);
                    var completed = await Task.WhenAny(timeout, remainingDepdencies);
                    if (completed == remainingDepdencies) // all dependencies received
                    {
                        cts.Cancel();
                        await context.CallActivityAsync(@"Publish-Event-Status", $"Ready to process {receivedEvent.Subject} : dependencies remaining {completed.Result.Count}");
                    }
                    else
                    {
                        // Timed out 
                        //signal to end await context.RaiseEventAsync(orchestration.InstanceId, @"Event-Aggregator-Orchestrator", eventGridEvent);
                        await context.CallActivityAsync(@"Publish-Event-Status", $"Ready to process {receivedEvent.Subject} : dependencies remaining {completed.Result.Count}");
                    }

                }
                else
                {
                    await context.CallActivityAsync(@"Publish-Event-Status", $"Ready to process {receivedEvent.Subject}");
                }
            }
        }

        [FunctionName("Publish-Event-Status")]
        public async Task PublishStatusAsync([ActivityTrigger] string status)
        {
            Logger.LogInformation($"Publishing Status for Event: {status}");
            await Task.CompletedTask;
        }

        private async Task<List<string>> DependenciesReceivedAsync(IDurableOrchestrationContext context, List<string> dependencies, CancellationTokenSource cts)
        {
            while (dependencies.Any())
            {
                // wait for dependencies to arrive
                var dependency = await context.WaitForExternalEvent<EventGridEvent>(@"Event-Aggregator-Orchestrator");
                if (dependency != null)
                {
                    if (dependency.Subject.Equals("Exit"))
                        return dependencies;

                    dependencies.Remove(dependency.EventType);
                }
            }
            return dependencies;
        }

        private async Task ReceiveOrUpdateEventsAsync(EventGridEvent eventGridEvent, IDurableClient client)
        {
            // Get orchestration Status
            var orchestration = await client.GetStatusAsync(eventGridEvent.Subject); // Subject is Unique for Testing
            if (orchestration == null)
            {
                var instance = await client.StartNewAsync(@"Event-Aggregator-Orchestrator", eventGridEvent.Subject, eventGridEvent);
                Logger.LogInformation($"Started new Orchestration instance {instance} for {orchestration}");
            }
            else
            {
                if (orchestration.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
                    Logger.LogError($"Cannot start new instance. {orchestration.Output} since already terminated.");
                else
                    await client.RaiseEventAsync(orchestration.InstanceId, @"Event-Aggregator-Orchestrator", eventGridEvent);
            }

        }
    }
}
