using RHToolkit.Properties;
using System.ComponentModel;

namespace RHToolkit.Models.Localization
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LocalizedDescriptionAttribute(string resourceKey) : DescriptionAttribute
    {
        private readonly string _resourceKey = resourceKey;

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
