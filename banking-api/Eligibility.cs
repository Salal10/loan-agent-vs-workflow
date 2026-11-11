namespace LoanApi;

// The bank's own copy of the pre-check rules in policy.md.
// Used to refuse ReadyForReview when a check fails, whoever asks for it.
public static class Eligibility
{
    public const int MinAge = 21;
    public const int MaxAgeAtEnd = 65;

    public static decimal DebtBurdenRatio(Customer customer, LoanApplication application)
    {
        var installment = LoanMath.MonthlyInstallment(application.Amount, application.TenureMonths);
        return Math.Round((customer.ExistingMonthlyDebt + installment) / customer.MonthlyIncome, 4);
    }

    // Returns why the application can't be marked ReadyForReview, or null if every check passes.
    public static string? ReasonNotReady(LoanApplication application, Customer customer, DateOnly today)
    {
        // 1. Identity
        if (customer.KycStatus != KycStatus.Verified || customer.CnicExpiry < today)
            return "KYC is not verified or the CNIC has expired.";

        // 2. Documents
        var incomeProof = customer.EmploymentType == EmploymentType.Salaried ? DocumentType.SalarySlip : DocumentType.TaxReturn;
        foreach (var type in new[] { DocumentType.Cnic, DocumentType.BankStatement, incomeProof })
        {
            var document = application.Documents.FirstOrDefault(d => d.Type == type);
            if (document is null || document.Status == DocumentStatus.Rejected)
                return $"{type} is missing or was rejected.";
            if (document.Status != DocumentStatus.Verified)
                return $"{type} has been received but is not verified yet.";
        }

        // 3. Age
        var ageAtEnd = AgeOn(customer.DateOfBirth, today.AddMonths(application.TenureMonths));
        if (AgeOn(customer.DateOfBirth, today) < MinAge || ageAtEnd > MaxAgeAtEnd)
            return $"The customer would be {ageAtEnd} at the end of the loan (limit {MaxAgeAtEnd}).";

        // 4. Debt burden ratio
        var dbr = DebtBurdenRatio(customer, application);
        if (dbr > LoanMath.DbrLimit)
            return $"The debt burden ratio is {dbr:P1}, above the {LoanMath.DbrLimit:P0} limit.";

        return null;
    }

    private static int AgeOn(DateOnly birth, DateOnly on)
    {
        var age = on.Year - birth.Year;
        if (on < birth.AddYears(age)) age--;
        return age;
    }
}
