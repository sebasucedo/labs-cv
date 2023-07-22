using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai.domain;

public class InputParameters
{
    public string QueryString { get; set; } = Constants.DEFAULT;
    public string As { get; set; } = string.Empty;
    public string Language { get; set; } = Constants.DEFAULT_LANGUAGE;
    public string Format { get; set; } = Constants.DEFAULT;
}