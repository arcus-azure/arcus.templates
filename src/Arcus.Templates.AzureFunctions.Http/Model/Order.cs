using System;
using System.ComponentModel.DataAnnotations;

namespace Arcus.Templates.AzureFunctions.Http.Model
{
    public class Order
    {
        [Required]
        public string Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ArticleNumber { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTimeOffset Scheduled { get; set; }
    }
}
