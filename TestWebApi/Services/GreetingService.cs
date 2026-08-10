using quantiva.framework.dependecyannotation;

namespace TestWebApi.Services;

[SingletonService]
public class GreetingService : IGreetingService
{
    public string Greet(string name)
    {
        return $"Hello, {name}! The time is {DateTime.Now:HH:mm:ss}.";
    }
}
