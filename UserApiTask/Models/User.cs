﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserApiTask.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "This field is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "This field is required")]
        public int Age { get; set; }

        [Required(ErrorMessage = "This field is required")]
        public string Email { get; set; }

        [ForeignKey("RoleId")]
        public List<Role>? Roles { get; set; }

    }
}
