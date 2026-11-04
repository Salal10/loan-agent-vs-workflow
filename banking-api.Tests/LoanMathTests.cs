using LoanApi;

namespace LoanApi.Tests;

public class LoanMathTests
{
    [Fact]
    public void Installment_for_one_million_over_24_months_is_52871()
    {
        var installment = LoanMath.MonthlyInstallment(1_000_000, 24);

        Assert.Equal(52_871m, installment);
    }

    [Theory]
    [InlineData(500_000, 24, 26_436)]
    [InlineData(1_500_000, 48, 48_903)]
    [InlineData(800_000, 36, 31_386)]
    public void Installment_matches_known_values(decimal amount, int months, decimal expected)
    {
        Assert.Equal(expected, LoanMath.MonthlyInstallment(amount, months));
    }

    [Fact]
    public void Longer_tenure_means_smaller_installment()
    {
        var shortLoan = LoanMath.MonthlyInstallment(500_000, 12);
        var longLoan = LoanMath.MonthlyInstallment(500_000, 60);

        Assert.True(longLoan < shortLoan);
    }
}