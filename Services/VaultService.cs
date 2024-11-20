using MongoDB.Driver;
using VaultShare.Models;

public class VaultService
{
    private readonly IMongoCollection<Vault> _vaultCollection;

    public VaultService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("VaultShareDB");
        _vaultCollection = database.GetCollection<Vault>("Vaults");
    }

    public async Task<Vault?> GetVaultByIdAsync(string vaultId)
    {
        return await _vaultCollection.Find(v => v.VaultId == vaultId).FirstOrDefaultAsync();
    }

    public async Task UpdateVaultAsync(Vault vault)
    {
        await _vaultCollection.ReplaceOneAsync(v => v.VaultId == vault.VaultId, vault);
    }
}
