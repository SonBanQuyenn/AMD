using System.Security.Cryptography;

namespace PollService.Api;

// Generates short codes like "7fGh2" for poll URLs (/poll/7fGh2).
// Good enough for this project's scope - not meant to be cryptographically perfect.
public static class ShortCodeGenerator
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Generate(int length = 6)
    {
        var chars = new char[length];
        var bytes = RandomNumberGenerator.GetBytes(length);

        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }
}
