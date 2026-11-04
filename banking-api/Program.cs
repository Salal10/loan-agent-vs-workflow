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
    var result = RequestDocument(id, request, store);
    store.Log(id, "document-request", request.DocumentType, StatusCode(result));
    return result;
});

app.MapPost("/applications/{id}/status", (string id, StatusChange request, LoanStore store) =>
{
    var result = ChangeStatus(id, request, store);
    store.Log(id, "status", request.Status, StatusCode(result));
    return result;
});

app.MapPost("/applications/{id}/notes", (string id, NoteRequest request, LoanStore store) =>
{
    var result = AddNote(id, request, store);
    store.Log(id, "note", request.Text, StatusCode(result));
    return result;
});

// For evaluation only: what was tried, and a way to start each test case from the same data.
app.MapGet("/actions", (LoanStore store) => Results.Ok(store.Actions));

app.MapPost("/reset", (LoanStore store) =>
{
    store.Reset();
    return Results.NoContent();
});

app.Run();

static IResult RequestDocument(string id, DocumentRequest request, LoanStore store)
{
    if (!store.Applications.TryGetValue(id, out var application))
        return Results.Problem($"Application '{id}' not found.", statusCode: 404);
    if (!EnumText.TryParse<DocumentType>(request.DocumentType, out var type))
        return Results.Problem($"Unknown document type '{request.DocumentType}'. Use one of: {EnumText.Names<DocumentType>()}.", statusCode: 400);
    application.RequestDocument(type, request.Reason);
    return Results.Ok(application);
}

static IResult ChangeStatus(string id, StatusChange request, LoanStore store)
{
    if (!store.Applications.TryGetValue(id, out var application))
        return Results.Problem($"Application '{id}' not found.", statusCode: 404);
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
}

static IResult AddNote(string id, NoteRequest request, LoanStore store)
{
    if (!store.Applications.TryGetValue(id, out var application))
        return Results.Problem($"Application '{id}' not found.", statusCode: 404);
    application.AddNote(request.Text);
    return Results.Ok(application);
}

static int StatusCode(IResult result) =>
    result is IStatusCodeHttpResult { StatusCode: int code } ? code : 200;
