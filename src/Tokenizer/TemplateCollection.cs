using System.Collections;
using System.Collections.Concurrent;

namespace Tokens;

/// <summary>
/// Collection of <see cref="Template" /> objects.
/// </summary>
public class TemplateCollection : IReadOnlyCollection<Template>
{
    private readonly ConcurrentDictionary<ulong, Template> templates;

    /// <summary>
    /// Returns the number of templates in this collection
    /// </summary>
    public int Count => templates.Count;

    /// <summary>
    /// Creates a new instance of the <see cref="TemplateCollection"/> class.
    /// </summary>
    public TemplateCollection()
    {
        templates = new ConcurrentDictionary<ulong, Template>();
    }

    /// <summary>
    /// Adds a template to the collection.
    /// If a template with the same Id already exists, it will be replaced.
    /// </summary>
    public void Add(Template template)
    {
        templates.AddOrUpdate(template.Id, template, (key, existing) => template);
    }

    /// <summary>
    /// Tries to get the template with the given Id.
    /// </summary>
    public bool TryGet(ulong id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Template? template)
    {
        return templates.TryGetValue(id, out template);
    }

    /// <summary>
    /// Tries to get the template with the given name (linear scan).
    /// </summary>
    public bool TryGet(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Template? template)
    {
        foreach (var candidate in templates.Values)
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                template = candidate;
                return true;
            }
        }

        template = null;
        return false;
    }

    /// <summary>
    /// Gets the template with the given name. Returns null if not found.
    /// </summary>
    public Template? Get(string name)
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
        foreach (var template in this)
        {
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
        foreach (var template in this)
        {
            if (template.HasTags(tags))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the templates in this collection.
    /// </summary>
    public IEnumerator<Template> GetEnumerator()
    {
        return templates.Values.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
