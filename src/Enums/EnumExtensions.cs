// https://github.com/darklinkpower/PlayniteExtensionsCollection/blob/16b3d2ebd4cd6e0ff7a39dc22380b24c72bcb392/source/Common/PluginsCommon/Extensions/EnumExtensions.cs

using Playnite.SDK;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace System
{
    public static class EnumExtensions
    {
        public static int GetMax(this Enum source)
        {
            return Enum.GetValues(source.GetType()).Cast<int>().Max();
        }
        public static int GetMin(this Enum source)
        {
            return Enum.GetValues(source.GetType()).Cast<int>().Min();
        }

        public static string GetDescription(this Enum source)
        {
            FieldInfo field = source.GetType().GetField(source.ToString());
            if (field == null)
            {
                return string.Empty;
            }

            var attributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                var desc = attributes[0].Description;
                if (desc.StartsWith("LOC"))
                {
                    return ResourceProvider.GetString(desc);
                }
                else
                {
                    return attributes[0].Description;
                }
            }
            else
            {
                return source.ToString();
            }
        }
    }
}