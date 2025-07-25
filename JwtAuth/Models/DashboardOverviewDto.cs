namespace JwtAuth.Models
{
    public class DashboardOverviewDto
    {
        public int TotalInStock { get; set; }
        public decimal TotalSalesValue { get; set; }
        public decimal InventoryValue { get; set; }
        public int LowStockCount { get; set; }
        public required HotProductDto HotProduct { get; set; }
    }

    public class HotProductDto
    {
        public required string ProductName { get; set; }
        public int UnitsSold { get; set; }
    }

}
