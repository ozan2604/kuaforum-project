using KuaforumAPI.Domain.Common;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KuaforumAPI.Domain.Entities
{
    public class ReviewImage : BaseEntity
    {
        public Guid ReviewId { get; set; }
        [ForeignKey("ReviewId")]
        public virtual Review Review { get; set; }

        public string Url { get; set; }
    }
}
