namespace PerfumeStore.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
        public List<SalesChartViewModel> SalesChart { get; set; } = new();
    }

    public class RecentOrderViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TopProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Image { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesChartViewModel
    {
        public string Date { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public int Orders { get; set; }
    }

    public class ReportsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal AverageOrderValue { get; set; }
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();
        public Dictionary<string, decimal> SalesByCategory { get; set; } = new();
    }
}
