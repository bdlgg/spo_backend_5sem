using System.ComponentModel.DataAnnotations;
namespace backend_spo.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Имя обязательно")]
    [MinLength(1, ErrorMessage = "Имя не может быть пустым")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Фамилия обязательна")]
    [MinLength(1, ErrorMessage = "Фамилия не может быть пустой")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Неккоректный формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
    public string Password { get; set; } = string.Empty;
}