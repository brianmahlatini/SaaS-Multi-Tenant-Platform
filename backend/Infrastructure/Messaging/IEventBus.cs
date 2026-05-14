namespace SaaS.Api.Infrastructure.Messaging;

public interface IEventBus
{
    Task PublishAsync(PlatformEvent platformEvent, CancellationToken cancellationToken = default);
}
