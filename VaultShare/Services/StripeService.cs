using Stripe;
using Stripe.Issuing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaultShare.Models;

public class StripeService
{
    private const string PublishableKey = "pk_test_51NPnspA7BfFFFS7FniesST5RFuiXKDCO35HmK2VJDFjGXCXcBmOHGFSHiPDpAZ517ERXi2ukzD4SEyt45bTlrbHR00fRz3yxfE";
    private const string SecretKey = "sk_test_51NPnspA7BfFFFS7FniesST5RFuiXKDCO35HmK2VJDFjGXCXcBmOHGFSHiPDpAZ517ERXi2ukzD4SEyt45bTlrbHR00fRz3yxfE";

    public StripeService()
    {
        StripeConfiguration.ApiKey = SecretKey;
    }

public async Task<Vault> CreateVirtualCardForVaultAsync(Vault vault, long spendingLimit)
{
    try
    {
        Console.WriteLine("Starting the process to create a virtual card for vault: " + vault.Name);

        // Attempt to create a cardholder in Stripe
        var cardholderOptions = new Stripe.Issuing.CardholderCreateOptions
        {
            Name = vault.Name,
            Email = $"{vault.Name}@vaultshare.com",
            Type = "company",
            Billing = new Stripe.Issuing.CardholderBillingOptions
            {
                Address = new Stripe.AddressOptions
                {
                    Line1 = "1234 Main Street",
                    City = "Anytown",
                    State = "CA",
                    Country = "US",
                    PostalCode = "12345",
                },
            },
        };
        var cardholderService = new Stripe.Issuing.CardholderService();
        var cardholder = await cardholderService.CreateAsync(cardholderOptions);
        Console.WriteLine("Cardholder created with ID: " + cardholder.Id);

        // Create virtual card for the cardholder
        var cardOptions = new Stripe.Issuing.CardCreateOptions
        {
            Cardholder = cardholder.Id,
            Currency = "usd",
            Type = "virtual",
            SpendingControls = new Stripe.Issuing.CardSpendingControlsOptions
            {
                SpendingLimits = new List<Stripe.Issuing.CardSpendingControlsSpendingLimitOptions>
                {
                    new Stripe.Issuing.CardSpendingControlsSpendingLimitOptions
                    {
                        Amount = spendingLimit,
                        Interval = "monthly",
                    },
                },
            },
        };
        var cardService = new Stripe.Issuing.CardService();
        var card = await cardService.CreateAsync(cardOptions);
        Console.WriteLine("Virtual card created with ID: " + card.Id);

        // Assign card details to the vault
        vault.CardId = card.Id;
        vault.CardNumber = "XXXX-XXXX-XXXX-" + card.Last4;
        vault.CardExpiry = $"{card.ExpMonth}/{card.ExpYear}";
        vault.CardCvc = "XXX"; // Stripe does not return actual CVC codes
        vault.CardNickname = "Vault Card";

        return vault;
    }
    catch (StripeException ex)
    {
        Console.WriteLine($"Stripe error creating virtual card: {ex.Message}");
        return GenerateMockCardData(vault);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error creating virtual card: {ex.Message}");
        return GenerateMockCardData(vault);
    }
}

// Modified GenerateMockCardData to accept the original vault object
private Vault GenerateMockCardData(Vault originalVault)
{
    Random rand = new Random();
    Console.WriteLine("Generating mock card data for vault: " + originalVault.Name);

    // Set mock card details, keeping existing VaultId and Name
    originalVault.CardId = "mock_card_" + Guid.NewGuid().ToString("N").Substring(0, 8);
    originalVault.CardNumber = $"{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}-{rand.Next(1000, 9999)}";
    originalVault.CardExpiry = $"{rand.Next(1, 12):D2}/{rand.Next(23, 30)}"; // Random month/year for expiry
    originalVault.CardCvc = rand.Next(100, 999).ToString();
    originalVault.CardNickname = "Vault Mock Card";

    return originalVault;
}}

