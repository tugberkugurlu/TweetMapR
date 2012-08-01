using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Hubs;

namespace TweetMapR.Infrastructure {

    public abstract class ConnectionAwareHub : Hub, IConnected, IDisconnect {

        public virtual Task Connect() {

            return TaskHelpers.NullResult();
        }

        public virtual Task Reconnect(IEnumerable<string> groups) {

            return TaskHelpers.NullResult();
        }

        public virtual Task Disconnect() {

            return TaskHelpers.NullResult();
        }
    }
}