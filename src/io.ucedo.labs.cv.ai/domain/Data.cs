using DotLiquid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai.domain;

public class Data
{
    public string Name { get; set; } = null!;
    public string Headline { get; set; } = null!;
    [JsonPropertyName("profile_picture_url")]
    public string ProfilePictureUrl { get; set; } = string.Empty;
    [JsonPropertyName("profile_picture_mask_url")]
    public string ProfilePictureMaskUrl { get; set; } = string.Empty;
    public string About { get; set; } = null!;
    public IEnumerable<InfoItem> Info { get; set; } = Enumerable.Empty<InfoItem>();
    public IEnumerable<Experience> Experiences { get; set; } = Enumerable.Empty<Experience>();
    public IEnumerable<EducationItem> Education { get; set; } = Enumerable.Empty<EducationItem>();
    public IEnumerable<Language> Languages { get; set; } = Enumerable.Empty<Language>();
    public IEnumerable<Certification> Certifications { get; set; } = Enumerable.Empty<Certification>();

    public class InfoItem : ILiquidizable
    {
        public string Href { get; set; } = null!;
        public string @Class { get; set; } = null!;
        public string Title { get; set; } = null!;
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
        public string Title { get; set; } = null!;
        public string Company { get; set; } = null!;
        public string Age { get; set; } = null!;
        public string Description { get; set; } = null!;

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
        public string Degree { get; set; } = null!;
        public string Institution { get; set; } = null!;
        public string Age { get; set; } = null!;
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
        public string Name { get; set; } = null!;
        public string Proficiency { get; set; } = null!;
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
        public string Name { get; set; } = null!;
        [JsonPropertyName("issuing_organization")]
        public string IssuingOrganization { get; set; } = null!;
        [JsonPropertyName("issue_date")]
        public string IssueDate { get; set; } = null!;
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
