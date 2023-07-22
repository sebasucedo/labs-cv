using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai.openai;

public class ImageResponse
{
    public long created { get; set; }
    public IEnumerable<Image> data { get; set; }

    public class Image
    {
        public string url { get; set; }
    }
}
