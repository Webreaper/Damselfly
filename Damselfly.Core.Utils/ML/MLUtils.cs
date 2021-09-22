using System;
using System.IO;
using System.Reflection;

namespace Damselfly.Core.Utils.ML
{
    public static class MLUtils
    {
        public static DirectoryInfo? ModelFolder
        {
            get
            {
                var modelFolder = Path.Combine(".", "Models");

                if (!Directory.Exists(modelFolder))
                {
                    var asm = Assembly.GetExecutingAssembly();

                    if (asm != null)
                    {
                        var thisAsm = new FileInfo(asm.Location);

                        if (thisAsm != null && thisAsm.Directory != null)
                        {
                            modelFolder = Path.Combine(thisAsm.Directory.FullName, "Models");
                        }
                    }
                }

                if (Directory.Exists(modelFolder))
                {
                    return new DirectoryInfo(modelFolder);
                }

                Logging.LogError($"Models folder not found: {modelFolder}");

                return null;
            }
        }
    }
}
