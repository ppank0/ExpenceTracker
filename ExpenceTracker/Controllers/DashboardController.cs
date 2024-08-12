using ExpenceTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExpenceTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDBContex _context;

        public DashboardController(ApplicationDBContex contex)
        {
                _context = contex;
        }
        public async Task<IActionResult> Index()
        {
            //Last 7  days transactions
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> Transaction = await _context.Transactions.Include(x => x.Category).ToListAsync();
            List<Transaction> SelectedTransaction = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

            //Total income
            int TotalIncome = Transaction
                .Where(x => x.Category.Type == "Income")
                .Sum(y => y.Amount);
            //ViewBag.TotalIncome = String.Format(culture, "{0:C0}", TotalIncome);

            //Weekly expense
            int WeeklyExpense = SelectedTransaction
                .Where(x => x.Category.Type == "Expense")
                .Sum(y => y.Amount);
            ViewBag.WeeklyExpense = String.Format(culture, "{0:C0}", WeeklyExpense);

            //Balance 
            int Balance = TotalIncome - Transaction
                .Where(x => x.Category.Type == "Expense")
                .Sum(y => y.Amount); 
            
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);


            //Doughnut Chart Expense By Category
            ViewBag.DoughnutChartData = SelectedTransaction
                .Where(x => x.Category.Type == "Expense")
                .GroupBy(y => y.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(y => y.Amount),
                    formattedAmount = k.Sum(y => y.Amount).ToString(culture).Insert(0, "$ "),
                }).OrderByDescending(k=>k.amount).ToList();

            //Spline Chart - Income vs Expense

            //Income
            List<SplineChartData> IncomeSummary = SelectedTransaction
                .Where(x => x.Category.Type == "Income")
                .GroupBy(y => y.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(x=>x.Amount),
                    
                }).ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransaction
                .Where(x => x.Category.Type == "Expense")
                .GroupBy(y => y.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(x => x.Amount),

                }).ToList();


            //Combine Income & Expense

            string[] last7Days = Enumerable.Range(0, 7)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into ExpenseJoined
                                      from expense in ExpenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            //Recent Transactions

            ViewBag.RecentTransactions = await _context.Transactions
                .Include(x => x.Category)
                .OrderByDescending(y => y.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
