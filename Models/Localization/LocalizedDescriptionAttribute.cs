using System.ComponentModel;

namespace RHToolkit.Models.Localization
{
    /// <summary>
    /// Provides a localized description for an enumeration field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LocalizedDescriptionAttribute(string resourceKey) : DescriptionAttribute
    {
        private readonly string _resourceKey = resourceKey;

        /// <summary>
        /// Gets the localized description from the resource manager.
        /// </summary>
        public override string Description
        {
            get
            {
                string? description = Resources.ResourceManager.GetString(_resourceKey);
                return description ?? $"[Missing Resource: {_resourceKey}]";
            }
        }
    }
}