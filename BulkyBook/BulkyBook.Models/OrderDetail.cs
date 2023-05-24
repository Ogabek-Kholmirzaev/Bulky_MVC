using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BulkyBook.Models;

public class OrderDetail
{
    public int Id { get; set; }

    [Required]
    public int OrderHeaderId { get; set; }
    [ForeignKey(nameof(OrderHeaderId))]
    [ValidateNever]
    public virtual OrderHeader OrderHeader { get; set; }


    [Required]
    public int ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    [ValidateNever]
    public virtual Product Product { get; set; }


    public int Count { get; set; }
    public double Price { get; set; }
}