using System.ComponentModel.DataAnnotations;

namespace backend_spo.Entities;

public class CarPhoto
{
    public int Id { get; set; }
    
    public int CarId { get; set; }
    
    public Car Car { get; set; } = null!;

    [Required] public string Url { get; set; } = null!;
    
    public int SortOrder { get; set; }
}