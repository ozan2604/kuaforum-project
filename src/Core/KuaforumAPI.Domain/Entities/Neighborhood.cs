namespace KuaforumAPI.Domain.Entities
{
    public class Neighborhood
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int DistrictId { get; set; }
        public District District { get; set; }
    }
}
