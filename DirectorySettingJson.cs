using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryCleaner
{
    public class DirectorySettingRuleJson
    {
        [JsonProperty("label")]
        public string? Label { get; set; }
        [JsonProperty("dir")]
        public string Directory { get; set; }
        [JsonProperty("rgx")]
        public string? RegExPattern { get; set; }
        [JsonProperty("flags")]
        public string? Flags { get; set; }
    }

    public class DirectorySettingJson
    {
        [JsonProperty("rules")]
        public List<DirectorySettingRuleJson> Rules { get; set;}
    }
}
