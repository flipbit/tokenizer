using System.Collections;
using System.Collections.Concurrent;

namespace Tokens;

/// <summary>
/// Collection of <see cref="Template" /> objects.
/// </summary>
public sealed class TemplateCollection : IReadOnlyCollection<Template>
{
    private readonly ConcurrentDictionary<ulong, Template> _templates;

    /// <summary>
    /// Returns the number of _templates in this collection
    /// </summary>
    public int Count => _templates.Count;

    /// <summary>
    /// Creates a new instance of the <see cref="TemplateCollection"/> class.
    /// </summary>
    public TemplateCollection()
    {
        _templates = new ConcurrentDictionary<ulong, Template>();
    }

    /// <summary>
    /// Adds a template to the collection.
    /// If a template with the same Id already exists, it will be replaced.
    /// </summary>
    public void Add(Template template)
    {
#if NET8_0_OR_GREATER
        _templates.AddOrUpdate(template.Id, (key, t) => t, (key, existing, t) => t, template);
#else
        _templates.AddOrUpdate(template.Id, template, (key, existing) => template);
#endif
    }

    /// <summary>
    /// Tries to get the template with the given Id.
    /// </summary>
    public bool TryGet(ulong id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Template? template)
    {
        return _templates.TryGetValue(id, out template);
    }

    /// <summary>
    /// Tries to get the template with the given name (linear scan).
    /// </summary>
    public bool TryGet(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Template? template)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var candidate in _templates.Values)
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
    /// Clears all _templates from this collection
    /// </summary>
    public void Clear()
    {
        _templates.Clear();
    }

    /// <summary>
    /// Determines if any _templates are in this collection that contain the given
    /// tag.
    /// </summary>
    public bool ContainsTag(string tag)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
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
    /// Determines if any _templates in this collection contain all the given tags.
    /// </summary>
    public bool ContainsAllTags(params string[] tags)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
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
    /// Returns an enumerator that iterates through the _templates in this collection.
    /// </summary>
    public IEnumerator<Template> GetEnumerator()
    {
        return _templates.Values.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
