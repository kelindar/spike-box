using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spike.Box
{
    /// <summary>
    /// Represents an event queue.
    /// </summary>
    public sealed class ThreadQueue
    {
        /// <summary>
        /// The execution queue.
        /// </summary>
        private readonly ConcurrentQueue<Action> Queue 
            = new ConcurrentQueue<Action>();

        public void Enqueue()
        {


        }

    }

    /// <summary>
    /// Represents an action queue.
    /// </summary>
    internal struct EventAction
    {
        public EventAction(Action action, Channel channel)
        {
            this.Action = action;
            this.Channel = channel;
        }

        Action Action;
        Channel Channel;


    }
}
