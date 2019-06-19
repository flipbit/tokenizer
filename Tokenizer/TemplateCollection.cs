using System;
using System.Collections.Generic;
using System.Linq;

namespace Tokens
{
    /// <summary>
    /// Collection of <see cref="Template" /> objects.
    /// </summary>
    public class TemplateCollection : List<Template>
    {
        public bool ContainsTag(string tag)
        {
            return this.Any(t => t.Tags.Any(templateTag => string.Compare(tag, templateTag, StringComparison.InvariantCultureIgnoreCase) == 0));
        }

        public bool ContainsAllTags(params string[] tags)
        {
            return this.Any(t => t.HasTags(tags));
        }
    }
}
