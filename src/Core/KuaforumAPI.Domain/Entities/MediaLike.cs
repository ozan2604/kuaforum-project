using KuaforumAPI.Domain.Common;

namespace KuaforumAPI.Domain.Entities
{
    /// <summary>ShopImage veya ShopVideo için kullanıcı beğenisi.</summary>
    public class MediaLike : BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        /// <summary>ShopImage.Id veya ShopVideo.Id</summary>
        public Guid MediaItemId { get; set; }

        /// <summary>"image" veya "video"</summary>
        public string MediaItemType { get; set; }
    }
}
