using System.ComponentModel.DataAnnotations;

namespace KuaforumAPI.Application.DTOs.Block
{
    public class BlockCustomerDto
    {
        [Required(ErrorMessage = "Müşteri kimliği zorunludur.")]
        public string CustomerId { get; set; }

        [MaxLength(500, ErrorMessage = "Sebep en fazla 500 karakter olabilir.")]
        public string? Reason { get; set; }
    }
}
