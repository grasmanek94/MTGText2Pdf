using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTGText2Pdf
{
    public interface ImageAble
    {
        public string GetName();
        public string[] GetImageUrls();
        public DateTime GetReleaseDate();

        public string GetImageFileName();

        public void SetCachedImage(string file);
        public string GetCachedImage();
    }
}
