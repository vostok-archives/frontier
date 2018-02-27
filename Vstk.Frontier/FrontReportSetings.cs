using System.Collections.Generic;
using System.Linq;

namespace Vstk.Frontier
{
    public class FrontierSetings
    {
        private string[] domainWhitelist;
        private HashSet<string> domainWhitelistHashSet;

        private string[] sourceMapBlacklist;
        private HashSet<string> sourceMapBlacklistHashSet;

        public string[] DomainWhitelist
        {
            get { return domainWhitelist; }
            set
            {
                domainWhitelist = value;
                domainWhitelistHashSet = value?.ToHashSet();
            }
        }

        public bool IsAllowedDomain(string domain)
        {
            return domainWhitelistHashSet == null || domainWhitelistHashSet.Count == 0 || domainWhitelistHashSet.Contains(domain);
        }

        public string[] SourceMapBlacklist
        {
            get { return sourceMapBlacklist; }
            set
            {
                sourceMapBlacklist = value;
                sourceMapBlacklistHashSet = value?.ToHashSet();
            }
        }

        public bool IsAllowedDomainForSourceMap(string domain)
        {
            return sourceMapBlacklistHashSet == null || !sourceMapBlacklistHashSet.Contains(domain);
        }
    }
}