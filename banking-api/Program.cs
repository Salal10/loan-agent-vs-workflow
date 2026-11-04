using System.Text.Json.Serialization;
using LoanApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(new LoanStore(DateOnly.FromDateTime(DateTime.Today)));

var app = builder.Build();

app.MapGet("/customers/{id}", (string id, LoanStore store) =>
    store.Customers.TryGetValue(id, out var customer) ? Results.Ok(customer) : Results.NotFound());

app.MapGet("/applications/{id}", (string id, LoanStore store) =>
    store.Applications.TryGetValue(id, out var application) ? Results.Ok(application) : Results.NotFound());

app.MapGet("/installment", (decimal amount, int months) =>
    Results.Ok(new { amount, months, annualRate = LoanMath.AnnualRate, monthlyInstallment = LoanMath.MonthlyInstallment(amount, months) }));

app.MapPost("/applications/{id}/document-requests", (string id, DocumentRequest request, LoanStore store) =>
{
    if (!store.Applications.TryGetValue(id, out var application)) return Results.NotFound();
    if (!EnumText.TryParse<DocumentType>(request.DocumentType, out var type))
        return Results.Problem($"Unknown document type '{request.DocumentType}'. Use one of: {EnumText.Names<DocumentType>()}.", statusCode: 400);
    application.RequestDocument(type, request.Reason);
    return Results.Ok(application);
});

app.MapPost("/applications/{id}/status", (string id, StatusChange request, LoanStore store) =>
{
    if (!store.Applications.TryGetValue(id, out var application)) return Results.NotFound();
    if (request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
        return Results.Problem("Loans can only be approved by a loan officer.", statusCode: 403);
    if (!EnumText.TryParse<ApplicationStatus>(request.Status, out var status))
        return Results.Problem($"Unknown status '{request.Status}'. Use ReadyForReview or NotEligible.", statusCode: 400);
    try
    {
        application.SetStatus(status, request.Reason);
    }
    catch (InvalidOperationException e)
    {
        return Results.Problem(e.Message, statusCode: 400);
    }
    return Results.Ok(application);
});

app.MapPost("/applications/{id}/notes", (string id, NoteRequest request, LoanStore store) =>
{
    if (!store.Applications.TryGetValue(id, out var application)) return Results.NotFound();
    application.AddNote(request.Text);
    return Results.Ok(application);
});

app.Run();