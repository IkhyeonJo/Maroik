namespace Maroik.Core.Contract.Dtos;

public class BoardResponse
{
    public long Id { get; set; }
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Writer { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public long View { get; set; }
    public bool Deleted { get; set; }
    public bool Locked { get; set; }
    public bool Noticed { get; set; }
}