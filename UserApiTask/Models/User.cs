using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserApiTask.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name field is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Age field is required")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Email field is required")]
        public string Email { get; set; }

        public List<Role>? Roles { get; set; }

    }
}
