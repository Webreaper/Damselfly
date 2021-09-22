using System;
using System.Linq;

namespace Damselfly.Core.Utils
{
    /// <summary>
    /// https://stackoverflow.com/questions/3445784/copy-the-property-values-to-another-object-with-c-sharp
    /// </summary>
    public static class ObjectUtils
    {
        public static bool CopyPropertiesTo<T, TU>(this T source, TU dest)
        {
            bool changed = false;
            var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof(TU).GetProperties().Where(x => x.CanWrite).ToList();

            foreach (var sourceProp in sourceProps)
            {
                var destProp = destProps.FirstOrDefault(x => x.Name == sourceProp.Name);

                if (destProp != null )
                {
                    var newVal = sourceProp.GetValue(source, null);
                    var prevVal = destProp.GetValue(dest, null);

                    if (newVal == null && prevVal == null)
                        continue;

                    if( newVal == null )
                    {
                        destProp.SetValue(dest, null);
                        Logging.LogVerbose($"Setting property {destProp.Name} to NULL");
                        changed = true;
                        continue;
                    }

                    if( ! newVal.Equals( prevVal ) )
                    {
                        Logging.LogVerbose($"Setting property {destProp.Name} to {newVal}");
                        // check if the property can be set or no.
                        destProp.SetValue(dest, newVal);
                        changed = true;
                    }
                }
            }

            return changed;
        }
    }
}

