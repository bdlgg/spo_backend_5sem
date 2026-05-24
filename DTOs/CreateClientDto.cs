using System.ComponentModel.DataAnnotations;

namespace backend_spo.DTOs;

public class CreateClientDto
{
    [Required(ErrorMessage = "Имя обязательно")]
    [MinLength(1, ErrorMessage = "Имя не может быть пустым")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Фамилия обязательна")]
    [MinLength(1, ErrorMessage = "Фамилия не может быть пустой")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефон обязателен")]
    [MinLength(1, ErrorMessage = "Телефон не может быть пустым")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;
}