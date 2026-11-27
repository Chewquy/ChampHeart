using System.Text.Json;

namespace ChampHeart.MVC.Models;

public static class SessionExtensions
{
    // Store an object in session as JSON
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    // Read an object back from session
    public static T? GetObject<T>(this ISession session, string key)
    {
        var json = session.GetString(key);

        if (string.IsNullOrEmpty(json))
        {
            return default(T);
        }

        return JsonSerializer.Deserialize<T>(json);
    }
}
