using ChampHeart.MVC.DTOs;

namespace ChampHeart.MVC.Models;

// Wrapper that keeps the JWT returned by the API in session state
public class ChampHeartSession
{
    private const string TokenKey = "token";

    private ISession session { get; set; }

    public ChampHeartSession(ISession sess) => session = sess;

    public TokenResponseDTO? GetToken() =>
        session.GetObject<TokenResponseDTO>(TokenKey);

    public void SetToken(TokenResponseDTO token) =>
        session.SetObject(TokenKey, token);

    public void RemoveToken() => session.Remove(TokenKey);
}
