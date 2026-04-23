namespace KuaforumAPI.Domain.Entities
{
    public class ShopCategoryAssignment
    {
        public Guid ShopId { get; set; }
        public int CategoryValue { get; set; }
        public virtual Shop Shop { get; set; }
    }
}
