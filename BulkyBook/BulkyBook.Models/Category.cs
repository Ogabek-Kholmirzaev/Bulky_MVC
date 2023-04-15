using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#pragma warning disable CS8618

namespace BulkyBook.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    [DisplayName("Category Name")]
    public string Name { get; set; }

    [Required]
    [Range(1, 100)]
    [DisplayName("Display Order")]
    public int DisplayOrder { get; set; }
}