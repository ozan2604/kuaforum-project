namespace KuaforumAPI.Domain.Entities
{
    public class SalonApplicationCategoryItem
    {
        public Guid ApplicationId { get; set; }
        public int CategoryValue { get; set; }
        public virtual SalonOwnerApplication Application { get; set; }
    }
}
