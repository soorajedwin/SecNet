using SecNetCore.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SecNetCore.Entities
{
    [SecEntity]
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        public bool IsActive { get; set; }

        [SecIgnore]
        public string? TemporaryValue { get; set; }
    }
}
