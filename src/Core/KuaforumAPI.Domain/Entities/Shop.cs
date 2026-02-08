using KuaforumAPI.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KuaforumAPI.Domain.Entities
{
    public class Shop : BaseEntity
    {
        public string OwnerId { get; set; }

        public virtual ApplicationUser Owner { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string District { get; set; }

        public string PhoneNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
