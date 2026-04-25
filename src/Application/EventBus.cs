namespace Application;

// Simple in-memory event bus (pub/sub)
public class EventBus
{
    private readonly Dictionary<string, List<Func<object, Task>>> _handlers = new();

    // Subscribe to an event
    public void Subscribe(string eventName, Func<object, Task> handler)
    {
        if (!_handlers.ContainsKey(eventName))
            _handlers[eventName] = new List<Func<object, Task>>();

        _handlers[eventName].Add(handler);
    }

    // Publish an event
    public async Task Publish(string eventName, object data)
    {
        if (!_handlers.ContainsKey(eventName))
            return;

        foreach (var handler in _handlers[eventName])
        {
            await handler(data);
        }
    }
}