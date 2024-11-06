using Stripe;
using Stripe.Issuing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaultShare.Models;


public class StripeService
{
    public StripeService()
    {
        Stripe.StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_API_KEY") ?? throw new Exception("Stripe API key not set");
    }

    public async Task<Stripe.Issuing.Card> CreateVirtualCardAsync(string cardholderId, long spendingLimit)
    {
        var cardOptions = new Stripe.Issuing.CardCreateOptions
        {
            Cardholder = cardholderId,
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
        Stripe.Issuing.Card card = await cardService.CreateAsync(cardOptions);
        return card;
    }
}
