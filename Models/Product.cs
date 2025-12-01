using System.ComponentModel.DataAnnotations;

namespace MVCandKAFKA3.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [StringLength(50)]
    public string Category { get; set; }

    [Required]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    public int Quantity { get; set; }

    [StringLength(50)]
    public string? Manufacturer { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public bool IsSentToKafka { get; set; } = false;
    public DateTime? SentToKafkaDate { get; set; }
    public string? KafkaStatus { get; set; } 
    public string? RejectionReason { get; set; }
}
public class PaginatedList<T>
{
    public List<T> Items { get; set; }
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }
}