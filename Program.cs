
using Stripe;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

var app = builder.Build();

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

app.MapPost("/create-checkout-session", async (IConfiguration config, HttpRequest req) =>
{
    var options = new Stripe.Checkout.SessionCreateOptions
    {
        PaymentMethodTypes = new List<string> { "card" },
        LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
        {
            new() { PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                {
                    UnitAmount = 2000, // amount in smallest currency unit
                    Currency = "usd",
                    ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions { Name = "Example Product" }
                }, Quantity = 1 }
        },
        Mode = "payment",
        SuccessUrl = "https://yourdomain.com/success?session_id={CHECKOUT_SESSION_ID}",
        CancelUrl = "https://yourdomain.com/cancel"
    };

    var service = new Stripe.Checkout.SessionService();
    var session = await service.CreateAsync(options);
    return Results.Json(new { sessionId = session.Id });
});

app.MapPost("/webhook", async (HttpRequest request) =>
{
    var json = await new StreamReader(request.Body).ReadToEndAsync();
    var sigHeader = request.Headers["Stripe-Signature"];
    var webhookSecret = builder.Configuration["Stripe:WebhookSecret"];

    try
    {
        var stripeEvent = EventUtility.ConstructEvent(json, sigHeader, webhookSecret);

        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            // TODO: fulfill the order, mark payment complete in DB
        }
        // handle other event types as needed

        return Results.Ok();
    }
    catch (StripeException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

app.Run();
