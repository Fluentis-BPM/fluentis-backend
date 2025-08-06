using System.ComponentModel.DataAnnotations;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.DTO
{
    public class InputCreateDto
    {
        public bool? EsJson { get; set; }
        [Required]
        public TipoInput TipoInput { get; set; }
    }

    public class InputUpdateDto
    {
        public bool? EsJson { get; set; }
        [Required]
        public TipoInput TipoInput { get; set; }
    }
}