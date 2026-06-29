using System.Collections.Generic;

namespace KuaforumAPI.Domain.Entities
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}
