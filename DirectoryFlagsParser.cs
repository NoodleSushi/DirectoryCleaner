namespace DirectoryCleaner
{
    public static class DirectoryFlagsParser
    {
        private static readonly Dictionary<string, DirectoryFlags> flagMap = new() 
        {
            {"-f", DirectoryFlags.Forced},
            {"--forced", DirectoryFlags.Forced},
            {"-rec", DirectoryFlags.Recursive},
            {"--recursive", DirectoryFlags.Recursive},
            {"-df", DirectoryFlags.DeleteFolder},
            {"--delete-folder", DirectoryFlags.DeleteFolder},
        };

        public static DirectoryFlags Parse(string[] typedFlags)
        {
            var flags = DirectoryFlags.None;
            foreach (var typedFlag in typedFlags)
            {
                flags |= Parse(typedFlag);
            }
            return flags;
        }

        public static DirectoryFlags Parse(string typedFlag)
        {
            return flagMap.GetValueOrDefault(typedFlag, DirectoryFlags.None);
        }
    }
}
