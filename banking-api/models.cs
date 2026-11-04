namespace LoanApi;

public enum EmploymentType { Salaried, SelfEmployed }
public enum KycStatus { Verified, Pending, Expired }
public enum DocumentType { Cnic, SalarySlip, BankStatement, TaxReturn }
public enum DocumentStatus { Received, Verified, Rejected }
public enum ApplicationStatus { Submitted, DocumentsRequested, ReadyForReview, NotEligible }

public record Customer(
    string Id,
    string Name,
    DateOnly DateOfBirth,
    EmploymentType EmploymentType,
    decimal MonthlyIncome,
    decimal ExistingMonthlyDebt,
    KycStatus KycStatus,
    DateOnly CnicExpiry);

public record Document(DocumentType Type, DocumentStatus Status, DateOnly UploadedOn);

public class LoanApplication
{
    public required string Id { get; init; }
    public required string CustomerId { get; init; }
    public required decimal Amount { get; init; }
    public required int TenureMonths { get; init; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public List<Document> Documents { get; init; } = [];
    public List<string> Notes { get; init; } = [];

    public void RequestDocument(DocumentType type, string reason)
    {
        Status = ApplicationStatus.DocumentsRequested;
        Notes.Add($"Requested {type}: {reason}");
    }

    public void SetStatus(ApplicationStatus status, string reason)
    {
        if (status is not (ApplicationStatus.ReadyForReview or ApplicationStatus.NotEligible))
            throw new InvalidOperationException($"The assistant can't set the status to {status}.");
        Status = status;
        Notes.Add($"Status set to {status}: {reason}");
    }

    public void AddNote(string text) => Notes.Add(text);
}

public record DocumentRequest(string DocumentType, string Reason);
public record StatusChange(string Status, string Reason);
public record NoteRequest(string Text);
public record ActionRecord(string ApplicationId, string Action, string Value, int StatusCode);

public static class EnumText
{
    public static bool TryParse<T>(string text, out T value) where T : struct, Enum =>
        Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value) && !int.TryParse(text, out _);

    public static string Names<T>() where T : struct, Enum => string.Join(", ", Enum.GetNames<T>());
}