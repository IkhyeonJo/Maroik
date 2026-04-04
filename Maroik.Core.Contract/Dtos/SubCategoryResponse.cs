namespace Maroik.Core.Contract.Dtos;

public class SubCategoryResponse
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? IconPath { get; set; }
    public string? Action { get; set; }
    public string? Role { get; set; }
    public long Order { get; set; }
}