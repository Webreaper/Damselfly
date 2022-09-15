using System;
using System.IO;
using System.Reflection;

namespace Damselfly.Core.Utils.ML;

public static class MLUtils
{
    public static DirectoryInfo? ModelFolder
    {
        get
        {
            var modelFolder = Path.Combine(".", "Models");

            try
            {
                if ( !Directory.Exists(modelFolder) )
                {
                    var asm = Assembly.GetExecutingAssembly();

                    if ( asm != null )
                    {
                        Logging.Log($"Looking for ML models in {asm.Location}...");

                        if ( File.Exists(asm.Location) )
                        {
                            var thisAsm = new FileInfo(asm.Location);

                            if ( thisAsm != null && thisAsm.Directory != null )
                                modelFolder = Path.Combine(thisAsm.Directory.FullName, "Models");
                        }
                    }
                }

                if ( Directory.Exists(modelFolder) ) return new DirectoryInfo(modelFolder);
            }
            catch ( Exception ex )
            {
                Logging.LogError($"Exception evaluating models folder: {ex.Message}");
            }

            Logging.LogError($"Models folder not found: {modelFolder}");

            return null;
        }
    }
}