using Simplic.MessageBroker;

namespace Simplic.Ox.CLI.Dummy
{
    public class MessageBus : IMessageBus
    {
        public void Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            throw new NotImplementedException("Dummy MessageBus");
        }

        public void Publish(object message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Dummy MessageBus");
        }

        public void Publish(object message, Type messageType, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Dummy MessageBus");
        }

        public void Publish<T>(object values, CancellationToken cancellationToken = default) where T : class
        {
            throw new NotImplementedException("Dummy MessageBus");
        }

        public void Send<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            throw new NotImplementedException("Dummy MessageBus");
        }

        public void Send(object message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Dummy MessageBus");
        }

        public void Send(object message, Type messageType, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Dummy MessageBus");
        }

        public void Send<T>(object values, CancellationToken cancellationToken = default) where T : class
        {
            throw new NotImplementedException("Dummy MessageBus");
        }
    }
}
