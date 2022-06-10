//using Newtonsoft.Json;
//using System.Collections.Generic;

//namespace Azure.DurableFunctions.EventAggregator
//{
//    public interface IEventAggregatorEntity
//    {
//        Task EventReceivedAsync(EventGridEvent eventGridEvent);
//    }

//    [JsonObject(MemberSerialization.OptIn)]
//    public class EventAggregatorEntity : IEventAggregatorEntity
//    {
//        private readonly ILogger<EventAggregatorEntity> logger;


//        [JsonProperty] 
//        public string SubjectId { get; set; }

//        [JsonProperty]
//        public List<string> Dependencies { get; set; }

//        public EventAggregatorEntity(string subjectid, ILogger<EventAggregatorEntity> loger, List<string> dependencies)
//        {
//            SubjectId = subjectid;
//            logger = loger;
//            Dependencies = dependencies;
//        }
        
//        public async Task EventReceivedAsync(EventGridEvent eventGridEvent)
//        {
//            await Task.CompletedTask;
//        }

//        // Boilerplate (entry point for the functions runtime)
//        [FunctionName(nameof(EventAggregatorEntity))]
//        public static Task HandleEntityOperation([EntityTrigger] IDurableEntityContext context, ILogger<EventAggregatorEntity> logger)
//        {
//            var eventReceived = context.GetInput<EventGridEvent>();
//            return context.DispatchAsync<EventAggregatorEntity>(eventReceived.Subject, logger);
//        }
//    }
//}
