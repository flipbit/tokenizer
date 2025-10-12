using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Tokens
{
    /// <summary>
    /// Collection of <see cref="Template" /> objects.
    /// </summary>
    public class TemplateCollection
    {
        private readonly ConcurrentDictionary<string, Template> templates;

        /// <summary>
        /// Returns the names of the templates in this collection
        /// </summary>
        public IList<string> Names => templates.Keys.ToList();

        /// <summary>
        /// Returns the number of templates in this collection
        /// </summary>
        public int Count => templates.Count;

        /// <summary>
        /// Creates a new instance of the <see cref="TemplateCollection"/> class.
        /// </summary>
        public TemplateCollection()
        {
            templates = new ConcurrentDictionary<string, Template>();
        }

        /// <summary>
        /// Adds a template to the collection.
        /// If a template with the same name already exists, it will be replaced.
        /// </summary>
        /// <param name="template"></param>
        public void Add(Template template)
        {
            templates.AddOrUpdate(template.Name, template, (key, existing) => template);
        }

        /// <summary>
        /// Tries to get the template with the given name.  If the template exists,
        /// will return true with the template set as <param>template</param>.
        /// If the template doesn't exist, will return false.
        /// </summary>
        public bool TryGet(string name, out Template template)
        {
            return templates.TryGetValue(name, out template);
        }

        /// <summary>
        /// Gets the template with the given name.  If the template doesn't exist,
        /// will return null.
        /// </summary>
        public Template Get(string name)
        {
            return TryGet(name, out var template) ? template : null;
        }

        /// <summary>
        /// Clears all templates from this collection
        /// </summary>
        public void Clear()
        {
            templates.Clear();
        }

        /// <summary>
        /// Determines if any templates are in this collection that contain the given
        /// tag.
        /// </summary>
        public bool ContainsTag(string tag)
        {
            foreach (var name in Names)
            {
                if (!TryGet(name, out var template)) continue;

                if (template.HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if any templates in this collection contain all the given tags.
        /// </summary>
        public bool ContainsAllTags(params string[] tags)
        {
            foreach (var name in Names)
            {
                if (!TryGet(name, out var template)) continue;

                if (template.HasTags(tags))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
