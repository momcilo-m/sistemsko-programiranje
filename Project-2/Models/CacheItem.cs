using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Project_2.Models
{
    public class CacheItem
    {
        public Image<Rgba32> Image { get; set; }
        public DateTime ExpiredTime { get; set; }

        public CacheItem(Image<Rgba32> img, DateTime expTime)
        {
            Image = img;
            ExpiredTime = expTime;
        }
    }
}
