namespace Maroik.Core.Contract.Dtos;

public class BoardCommentResponse
{
    public long Id { get; set; }
    public long BoardId { get; set; }
    public long Order { get; set; }
    public string? AvatarImagePath { get; set; }
    public string? Writer { get; set; }
    public string? Content { get; set; }
    public DateTime Created { get; set; }
    public bool Deleted { get; set; }
}