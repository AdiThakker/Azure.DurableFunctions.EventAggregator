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
        public async Task ReceiveEventsAsync([EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableClient client)
        {
            Logger.LogInformation($"Received Event: {eventGridEvent.Data.ToString()}");
            await ReceiveOrUpdateEventsAsync(eventGridEvent, client);
        }

        [FunctionName("Dependencies-Subscriber")]
        public async Task ReceiveDependenciesAsync([EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableClient client)
        {
            Logger.LogInformation($"Received Dependency: {eventGridEvent.Data.ToString()}");
            await ReceiveOrUpdateEventsAsync(eventGridEvent, client);
        }

        [FunctionName("Event-Aggregator-Orchestrator")]
        public async Task AggregateEventsAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                // Get the list of dependencies to aggregate for a given event
                var receivedEvent = context.GetInput<EventGridEvent>();
                string status = "";

                // Check if any dependencies     
                if (dependencies.TryGetValue(receivedEvent.Subject.ToString(), out List<string> dependenciesList))
                {
                    if (dependenciesList.Any())
                    {
                        using var cts = new CancellationTokenSource();
                        
                        // Create timer to wait for dependencies to be received
                        var endTime = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(30)); // Durable Timer                         
                        var timeoutTask = context.CreateTimer<List<string>>(endTime, default, cts.Token);

                        // Start tracking dependencies
                        var dependenciesRemaining = dependenciesList.ToList();
                        while (dependenciesRemaining.Any())
                        {
                            // wait for dependencies to arrive
                            var dependenciesTask = context.WaitForExternalEvent<EventGridEvent>(@"Event-Aggregator-Orchestrator");
                            var completedTask = await Task.WhenAny(timeoutTask, dependenciesTask);
                            if (completedTask == dependenciesTask)
                            {
                                if (dependenciesTask.Result != null)
                                {
                                    dependenciesRemaining.Remove(dependenciesTask.Result.EventType);
                                    if (dependenciesRemaining.Count == 0)
                                    {
                                        // All dependencies received
                                        status = "All dependencies received!";
                                        break;
                                    }
                                }
                            }
                            else if (completedTask == timeoutTask)
                            {
                                // Timeout
                                status = $"Timeout Occured, dependencies not received: {dependenciesList.Count}";
                                break;
                            }
                        }
                    }
                    else
                    {
                        status = "No dependencies, moving on!";
                    }
                }
                
                await context.CallActivityAsync(@"Event-Status-Publisher", status);
            }
            catch (TaskCanceledException ex)
            {
                if (!context.IsReplaying)
                    Logger.LogError(ex.ToString());
            }
            catch (Exception ex)
            {
                if (!context.IsReplaying)
                    Logger.LogError(ex.ToString());
            }
        }

        [FunctionName("Event-Status-Publisher")]
        public void PublishStatus([ActivityTrigger] string status)
        {
            // Logger.LogInformation(status);
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
                _ = orchestration.RuntimeStatus switch
                {
                    OrchestrationRuntimeStatus.Running => RaiseEventForOrchestration(),
                    _ => StartOrchestration()
                };
            }

            async Task RaiseEventForOrchestration()
            {
                await client.RaiseEventAsync(orchestration.InstanceId, @"Event-Aggregator-Orchestrator", eventGridEvent);                
            }

            async Task StartOrchestration()
            {
                var instance = await client.StartNewAsync(@"Event-Aggregator-Orchestrator", eventGridEvent.Subject, eventGridEvent);
                Logger.LogInformation($"Started new Orchestration instance {instance} for {orchestration}");
            }
        }
    }
}
