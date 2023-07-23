using DotLiquid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai.domain;

public class Data
{
    public string Name { get; set; }
    public string Headline { get; set; }
    public string About { get; set; }
    public List<InfoItem> Info { get; set; }
    public List<Experience> Experiences { get; set; }
    public List<EducationItem> Education { get; set; }
    public List<Language> Languages { get; set; }
    public List<Certification> Certifications { get; set; }

    public class InfoItem : ILiquidizable
    {
        public string Href { get; set; }
        public string @Class { get; set; }
        public string Title { get; set; }
        public object ToLiquid()
        {
            return new
            {
                Href,
                @Class,
                Title
            };
        }
    }

    public class Experience : ILiquidizable
    {
        public string Title { get; set; }
        public string Company { get; set; }
        public string Age { get; set; }
        public string Description { get; set; }

        public object ToLiquid()
        {
            return new
            {
                Title,
                Company,
                Age,
                Description,
            };
        }
    }

    public class EducationItem : ILiquidizable
    {
        public string Degree { get; set; }
        public string Institution { get; set; }
        public string Age { get; set; }
        public object ToLiquid()
        {
            return new
            {
                Degree,
                Institution,
                Age
            };
        }
    }

    public class Language : ILiquidizable
    {
        public string Name { get; set; }
        public string Proficiency { get; set; }
        public object ToLiquid()
        {
            return new
            {
                Name,
                Proficiency
            };
        }
    }

    public class Certification : ILiquidizable
    {
        public string Name { get; set; }
        [JsonPropertyName("issuing_organization")]
        public string IssuingOrganization { get; set; }
        [JsonPropertyName("issue_date")]
        public string IssueDate { get; set; }
        public object ToLiquid()
        {
            return new
            {
                Name,
                IssuingOrganization,
                IssueDate
            };
        }
    }
}
