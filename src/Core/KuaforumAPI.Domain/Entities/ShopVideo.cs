using KuaforumAPI.Domain.Common;
using System.Collections.Generic;

namespace KuaforumAPI.Domain.Entities
{
    /// <summary>
    /// Salon tanıtım videosu. Bir salon birden fazla video ekleyebilir.
    /// Şimdilik maksimum 1 video kısıtı service katmanında uygulanıyor.
    /// </summary>
    public class ShopVideo : BaseEntity
    {
        public Guid ShopId { get; set; }
        public virtual Shop Shop { get; set; }

        /// <summary>Cloudinary .mp4 URL'i</summary>
        public string Url { get; set; }

        /// <summary>Görüntüleme sırası (ilk video = 0)</summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>Videonun toplam izlenme sayısı</summary>
        public int ViewCount { get; set; } = 0;

        public virtual ICollection<ShopVideoTag> Tags { get; set; } = new List<ShopVideoTag>();
    }
}
