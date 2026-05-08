namespace backend_spo.DTOs;

public class CarPhotoResponseDto
{
    public int Id { get; set; }

    public string Url { get; set; } = null!;

    public int SortOrder { get; set; }
}