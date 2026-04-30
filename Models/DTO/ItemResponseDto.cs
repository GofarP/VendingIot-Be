public class ItemResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int ItemCategoryId { get; set; }
    public string ItemCategoryName { get; set; } = string.Empty;
}