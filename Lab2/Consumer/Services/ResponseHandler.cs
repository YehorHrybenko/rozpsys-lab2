
namespace Consumer.Services;

internal class ResponseHandler
{
    public Dictionary<Guid, Action<string>> listeners = [];

    public void AddResponse(Guid id, string result)
    {
        listeners[id].Invoke(result);
        listeners.Remove(id);
    }

    public void Subscribe(Guid id, Action<string> action)
    {
        listeners.Add(id, action);
    }

    public Task<string> PromiseRetrieve(Guid id)
    {
        var promise = new TaskCompletionSource<string>();

        Subscribe(id, promise.SetResult);

        return promise.Task;
    }
}
