namespace JwtAuth.Entity.Enums
{
    public enum ProductStatus
    {
        Available,
        OutOfStock,
        Discontinued
    }

    public enum ProductItemStatus
    {
        PendingStockIn,
        InStock,
        Sold,
        Damaged,
        Reserved,
        Lost
    }
    public enum TransactionType
    {
        StockIn,
        StockOut
    }
}
