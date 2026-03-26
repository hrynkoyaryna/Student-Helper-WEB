using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Models.Notes;

public class NotesIndexViewModel
{
    public List<Note> Notes { get; set; } = new();
    public string? SearchQuery { get; set; }
}
