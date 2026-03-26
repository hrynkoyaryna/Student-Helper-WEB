using System.ComponentModel.DataAnnotations;

namespace StudentHelper.Web.Models.Notes;

public class NoteCreateEditViewModel
{
    [Required(ErrorMessage = "Заголовок нотатки обов'язковий")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Заголовок має бути від 1 до 200 символів")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Текст нотатки обов'язковий")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "Текст має бути від 1 до 5000 символів")]
    public string Body { get; set; } = string.Empty;
}
