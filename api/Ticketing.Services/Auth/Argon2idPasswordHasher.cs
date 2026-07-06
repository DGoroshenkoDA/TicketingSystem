using Isopoh.Cryptography.Argon2;

namespace Ticketing.Services.Auth;

// Argon2id password hashing. Isopoh's default type is HybridAddressing (Argon2id);
// the encoded output embeds the salt and parameters, so Verify needs only the hash.
public class Argon2idPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => Argon2.Hash(password);

    public bool Verify(string password, string hash) => Argon2.Verify(hash, password);
}
