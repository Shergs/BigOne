public class KeyedSingletonRegistry
{
    private readonly Dictionary<string, object> _instances = new Dictionary<string, object>();

    public T GetOrCreate<T>(string key, Func<T> createInstance)
    {
        if (_instances.TryGetValue(key, out var instance))
        {
            return (T)instance;
        }

        var newInstance = createInstance();
        _instances[key] = newInstance;
        Console.WriteLine("Made one");
        return newInstance;
    }
}