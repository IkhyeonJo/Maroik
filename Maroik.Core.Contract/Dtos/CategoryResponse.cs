namespace Maroik.Core.Contract.Dtos;

public class CategoryResponse
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? IconPath { get; set; }
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public string? Role { get; set; }
    public long Order { get; set; }
}