﻿using Lightest.Data.Models.TaskModels;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Lightest.Data.Models
{
    public class CategoryUser : IAccessRights
    {
        [Required]
        public string UserId { get; set; }

        [JsonIgnore]
        public ApplicationUser User { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [JsonIgnore]
        public Category Category { get; set; }

        public bool CanRead { get; set; }

        public bool CanWrite { get; set; }

        public bool CanChangeAccess { get; set; }

        public bool IsOwner { get; set; }
    }
}