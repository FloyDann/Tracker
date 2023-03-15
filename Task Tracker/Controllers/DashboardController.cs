using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using Task_Tracker.Models;
using System.Globalization;

namespace Task_Tracker.Controllers
{ 
    public class DashboardController : Controller

    {

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
      
        public async Task<ActionResult> Index()
        {
            //Last 7 Days
            var StartDate = DateTime.Today.AddDays(-6);
            var EndDate = DateTime.Today;

            var SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            //Total Income
            var TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            var culture = CultureInfo.CreateSpecificCulture("en-Us");
            ViewBag.TotalIncome = TotalIncome.ToString("C0", culture);

            //Total Exp
            var TotalExp = SelectedTransactions
                .Where(i => i.Category.Type == "Exp")
                .Sum(j => j.Amount);
            ViewBag.TotalExp = TotalExp.ToString("C0", culture);

            //Balance
            var Balance = TotalIncome - TotalExp;
           // CultureInfo culture= CultureInfo.CreateSpecificCulture("en-Us");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = string.Format(culture , "{0:C0}" , Balance);

            //Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Exp")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon+" " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l=>l.amount)
                .ToList();
            var a = ViewBag.DoughnutChartData;
            //Spline Chart - Income vs Exp
            //Income
            var IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            //Exp
            var ExpSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Exp")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    exp = k.Sum(l => l.Amount)
                })
                .ToList();

            //Combine Income & Exp
            var Last7Days = Enumerable.Range(0, 7)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join exp in ExpSummary on day equals exp.day into dayExpJoined
                                      from exp in dayExpJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day,
                                          income = income == null ? 0 : income.income,
                                          exp = exp == null ? 0 : exp.exp,
                                      };

            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i=> i.Category)
                .OrderByDescending(j=> j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
    public class SplineChartData
    {
        public string? day;
        public int income;
        public int exp;
    }
}
