namespace LoanApi;

public static class LoanMath
{
    public const decimal AnnualRate = 0.24m;
    public const decimal DbrLimit = 0.40m;

    // Standard EMI formula: P * r * (1 + r)^n / ((1 + r)^n - 1), r = monthly rate
    public static decimal MonthlyInstallment(decimal amount, int months)
    {
        var r = (double)(AnnualRate / 12);
        var factor = Math.Pow(1 + r, months);
        var installment = (double)amount * r * factor / (factor - 1);
        return Math.Round((decimal)installment, 0);
    }
}