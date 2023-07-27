using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai;

public class Prompts
{
    public static string GetAboutPrompt(string about, string @as) => $"I need an about for my CV based on: {about}{@as}";
    public static string GetExperiencePrompt(string company, string title, string age, string description, string @as) =>
        @$"I need a brief resume, really short, for my position in {company} as {title} at this time period {age} based on: {description}{@as}";

    public static string GetProfilePicturePrompt(string @as) => $"In the following image of me, I need you to depict me as {@as}";
}