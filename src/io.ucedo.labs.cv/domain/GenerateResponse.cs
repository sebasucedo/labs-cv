using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.domain
{
    public class GenerateResponse
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string Content { get; set; } = string.Empty;
    }
}
