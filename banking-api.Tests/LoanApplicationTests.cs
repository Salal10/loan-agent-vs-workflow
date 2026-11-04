using LoanApi;

namespace LoanApi.Tests;

public class LoanApplicationTests
{
    private static LoanApplication NewApplication() =>
        new() { Id = "LA-TEST", CustomerId = "C-TEST", Amount = 100_000, TenureMonths = 12 };

    [Fact]
    public void Requesting_a_document_sets_status_and_adds_a_note()
    {
        var application = NewApplication();

        application.RequestDocument(DocumentType.SalarySlip, "Missing");

        Assert.Equal(ApplicationStatus.DocumentsRequested, application.Status);
        Assert.Single(application.Notes);
    }

    [Fact]
    public void Assistant_can_mark_ready_for_review()
    {
        var application = NewApplication();

        application.SetStatus(ApplicationStatus.ReadyForReview, "All checks pass");

        Assert.Equal(ApplicationStatus.ReadyForReview, application.Status);
    }

    [Fact]
    public void Assistant_cannot_set_status_back_to_submitted()
    {
        var application = NewApplication();

        Assert.Throws<InvalidOperationException>(() =>
            application.SetStatus(ApplicationStatus.Submitted, "Trying"));
    }

    [Fact]
    public void check_application_status()                         // ← 1. new name
    {
        var application = NewApplication();

        application.SetStatus(ApplicationStatus.NotEligible, "DBR too high"); // ← 2. NotEligible

        Assert.Equal(ApplicationStatus.NotEligible, application.Status);      // ← 3. NotEligible
    }
}