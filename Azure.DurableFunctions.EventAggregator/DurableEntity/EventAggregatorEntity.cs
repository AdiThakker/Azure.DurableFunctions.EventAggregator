using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.DurableFunctions.EventAggregator
{
    public interface IEventAggregatorEntity
    {
        Task EventReceivedAsync(EventGridEvent eventGridEvent);
    }

    public class EventAggregatorEntity : IEventAggregatorEntity
    {
        public async Task EventReceivedAsync(EventGridEvent eventGridEvent)
        {
            await Task.CompletedTask;
        }
    }
}
