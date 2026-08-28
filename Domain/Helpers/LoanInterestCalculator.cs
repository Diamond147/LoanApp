

using Domain.Entities;
using Domain.Enums;

namespace Domain.Helpers
{
    public static class LoanInterestCalculator
    {
        public static (decimal ProjectedAccruedInterest, DateTime? ProjectedAccrualDate) CalculateProjectedAccrual(Loan loan, DateTime asOfDate)
        {
            if (loan.Status != LoanStatus.Approved || loan.InterestRate == 0 || loan.PrincipalBalance == 0)
                return (loan.AccruedInterest, loan.LastInterestAccrualDate);

            var last = loan.LastInterestAccrualDate ?? loan.ApprovalDate ?? loan.RequestedDate;

            // Month & year difference calculation
            int monthsElapsed = ((asOfDate.Year - last.Year) * 12) + asOfDate.Month - last.Month;

            // Day difference calculation
            if (asOfDate.Day < last.Day) monthsElapsed--;

            if (monthsElapsed <= 0)
                return (loan.AccruedInterest, loan.LastInterestAccrualDate);

            // If the Interest rate is set on yearly basis.
            var interestForMonths = loan.PrincipalBalance * loan.InterestRate * monthsElapsed/12m;

            var projectedAccrued = loan.AccruedInterest + Math.Round((decimal)interestForMonths!, 2);
            var projectedDate = last.AddMonths(monthsElapsed);

            return (projectedAccrued, projectedDate);
        }
    }
}
