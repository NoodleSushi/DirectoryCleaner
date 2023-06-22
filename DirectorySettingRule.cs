using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryCleaner
{
    public class DirectorySettingRule
    {
        public DirectorySettingRuleJson Setting { get; private set; }
        public DirectoryFlags Flags { get; private set; } = DirectoryFlags.None;
        public DirectorySettingRule(DirectorySettingRuleJson directorySetting)
        {
            Setting = directorySetting;
            Flags = Setting.Flags?
                .Split(' ')
                .Select(x => DirectoryFlagsParser.Parse(x))
                .Aggregate((a, b) => a | b) ?? DirectoryFlags.None;
        }
    }
}
