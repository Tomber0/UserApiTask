using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UserApiTask.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [JsonIgnore]
        public List<User> Users { get; set; } = new();
    }
}
