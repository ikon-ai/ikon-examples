<!-- checked-against: 1e6b01b1ed9b29869c53615f -->

# Procountor Connector Guide

This guide covers `Ikon.Connectors.Procountor` — reading customers, suppliers, sales and purchase invoices and their payments from Procountor (Finago) — for app developers wiring a company's accounting figures into an Ikon app.

## Procountor

`Procountor` reads the business partner register, sales and purchase invoices and the payments recorded against them. It writes nothing. The types are in namespace `Ikon.Connectors`, not the package's name — `using Ikon.Connectors.Procountor;` names the `Procountor` class and fails with CS0138.

Procountor issues credentials in two halves. The **client id, client secret and redirect URI** identify your integration and come from Procountor when you request API access. The **API key** is created in Procountor by a user of the company whose figures you read — *Basics → API client keys → New API key*, entering your client id — and binds the connector to that user's rights in that one company; a company administrator can create it for a technical user that may use the API and not the application. Keep all four in `app.Secrets`. The connector exchanges the key for an access token and renews it five minutes before the lifetime Procountor gives it expires.

<!-- ikon-example: connectors-procountor -->
```csharp
var procountor = new Procountor(clientId, clientSecret, redirectUri, apiKey);

var customers = await procountor.ListCustomersAsync();
var invoices = await procountor.ListSalesInvoicesAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

foreach (var byStatus in invoices.GroupBy(invoice => invoice.Status))
{
    Log.Instance.Info($"{byStatus.Key}: {byStatus.Count()} invoices, {byStatus.Sum(invoice => invoice.Total ?? 0)} in accounting currency");
}

var payments = await procountor.ListPaymentEventsAsync(invoices[0].Id);
```

The constructor reads production (`Procountor.ProductionBaseUrl`) unless given another `baseUrl`; `Procountor.TestBaseUrl` is Procountor's public testing server, which has its own credentials and its own data. An empty credential, or a base URL that is not `https`, throws `ArgumentException` at construction.

A failure is a `ConnectorException` (from `Ikon.Connectors`) with `Provider` `"procountor"`; a network failure passes through as `HttpRequestException`. `IsReconnectRequired` (`StatusCode` `401`/`403`) means the API key or the integration's credentials were refused — a revoked key, which Procountor answers with `400`, is reported as `401` too — or the user it belongs to lacks the rights: have a person of the company issue a new key rather than retry. `Reason` carries Procountor's own explanation, such as `INVALID_API_KEY`. `IsTransient` (`408`, `429`, `5xx`) marks a call that may succeed later. A `2xx` response the connector cannot read is one with no `StatusCode`.

### Customers and suppliers

`ListCustomersAsync` and `ListSuppliersAsync` return the business partner register's customers and suppliers as `ProcountorBusinessPartner` records. Procountor lists active partners or deactivated ones, never both: `active: false` asks for the others.

### Invoices and payments

`ListSalesInvoicesAsync(startDate, endDate)` and `ListPurchaseInvoicesAsync(startDate, endDate)` return the invoices whose invoice date falls in the range, both ends included, as `ProcountorInvoice` records: `Id`, `PartnerId` and `PartnerName` (the customer on a sales invoice, the supplier on a purchase invoice), `InvoiceNumber`, `Date`, `DueDate`, `Total`, `TotalExcludingVat`, `Currency`, `Status` and `Type`; an `endDate` before `startDate` throws `ArgumentException` before any request. Three contracts to respect:

- **Every status comes back**, unfinished and invalidated invoices among them. `Status` is Procountor's own word (`UNFINISHED`, `NOT_SENT`, `SENT`, `PARTLY_PAID`, `PAID`, `MARKED_PAID`, `INVALIDATED` and more); decide which ones a figure counts before you sum.
- **`Total` is in the company's accounting currency**, also for an invoice issued in another currency, so totals add up. It is null where Procountor reported no such sum.
- **There is no open amount.** A listing carries `Status` and nothing about how much has been paid. `ListPaymentEventsAsync(invoiceId)` returns the payments recorded against one invoice as `ProcountorPaymentEvent` records; what is still owed is the total less the events you count as paid, and which of them count is an accounting decision the connector does not make.

Listings page by descending id, 200 rows a page, and a row created while a listing runs is never returned twice or made to hide another. The bound is `maxPages` (default 50, so 10,000 rows; 10 for `ListPaymentEventsAsync`; zero or less throws `ArgumentOutOfRangeException`): reaching it with a full last page throws `ConnectorPageCapException<T>` with the rows read as `Items` and the lowest id among them as `ResumeFrom` — parse it and pass it as `previousId` to read on.

### Rate limits

Procountor documents its request limits (60 a second in production, 90 a minute on the testing server) and no throttling response. The connector waits and retries on a `429` if one comes, up to three times after the `Retry-After` it names, and retries its token request the same way; a `Retry-After` beyond two minutes surfaces at once as a `429`.

### What it does not reach

Anything that writes (creating or sending invoices, recording payments, editing partners), single-entity reads, invoice rows and attachments, travel invoices and bills of charges, the ledger and its receipts, products, bank statements, and reports. One connector reads one company: the company is the one the API key was created in.
