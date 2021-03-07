using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ethermine_monitor
{
    public class MessageWriter : IMessageWriter
    {
        readonly ILogger<MessageWriter> _logger;

        public MessageWriter(ILogger<MessageWriter> logger)
        {
            _logger = logger;
        }

        public void Write(object message, bool log = true)
        {
            if (log) 
                Console.WriteLine(message);
           
                _logger.LogInformation(message.ToString());
        }
    }

    public interface IMessageWriter
    {
        void Write(object message, bool log = false);
    }
}
