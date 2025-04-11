using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.DbModels.Models;

public class SystemConfigSettings
{
    public WordpressSettings wpSettings { get; init; } = new();
    public CPULevelSettings cpuSettings { get; init; } = new();
    public SmtpSettings smtpSettings { get; init; } = new();
    public LoggingLevel serverLogLevel { get; set; }
    public bool importSidecarKeywords { get; set; }
    public bool useSmtp { get; set; } = true;
    public bool forceLogin { get; set; }
    public bool enableAIProcessing { get; set; } = false; // ConfigSettings.DefaultEnableRolesAndAuth;
    public bool disableObjectDetector { get; set; }
    public bool writeAITagsToImages { get; set; }
    public bool allowExternalRegistration { get; set; }
    public bool enableAuthAndRoles { get; set; } = true;
    public bool enableImageEditing { get; set; } = false;
    public bool enableBackgroundThumbs { get; set; } = false;
    public int similarityThreshold { get; set; } = 75;
    
    public void Save(IConfigService configService)
    {
        configService.Set(ConfigSettings.ImportSidecarKeywords, importSidecarKeywords.ToString());
        configService.Set(ConfigSettings.LogLevel, serverLogLevel.ToString());

        configService.Set(ConfigSettings.WordpressURL, wpSettings.URL);
        configService.Set(ConfigSettings.WordpressUser, wpSettings.UserName);
        configService.Set(ConfigSettings.WordpressPassword, wpSettings.Password);
        
        configService.Set(ConfigSettings.WriteAITagsToImages, writeAITagsToImages.ToString());
        configService.Set(ConfigSettings.EnableAIProcessing, enableAIProcessing.ToString());
        configService.Set(ConfigSettings.DisableObjectDetector, disableObjectDetector.ToString());
        configService.Set(ConfigSettings.SimilarityThreshold, similarityThreshold.ToString());
        configService.Set(ConfigSettings.UseSmtp, useSmtp.ToString());

        configService.Set(ConfigSettings.EnablePoliciesAndRoles, enableAuthAndRoles.ToString());
        configService.Set(ConfigSettings.EnableImageEditing, enableImageEditing.ToString());
        configService.Set( ConfigSettings.EnableBackgroundThumbs, enableBackgroundThumbs.ToString() );
        configService.Set(ConfigSettings.LogLevel, serverLogLevel.ToString());

        smtpSettings.Save(configService);
        cpuSettings.Save(configService);

        // If roles are disabled, disable forced Login
        var forceLoginProxy = enableAuthAndRoles && forceLogin;
        var extRegistrationProxy = enableAuthAndRoles && allowExternalRegistration;
        configService.Set(ConfigSettings.ForceLogin, forceLoginProxy.ToString());
        configService.Set(ConfigSettings.AllowExternalRegistration, extRegistrationProxy.ToString());
    }

    public void Load(IConfigService configService)
    {
        wpSettings.URL = configService.Get(ConfigSettings.WordpressURL);
        wpSettings.UserName = configService.Get(ConfigSettings.WordpressUser);
        wpSettings.Password = configService.Get(ConfigSettings.WordpressPassword);

        enableImageEditing = configService.GetBool(ConfigSettings.EnableImageEditing);
        enableBackgroundThumbs = configService.GetBool( ConfigSettings.EnableBackgroundThumbs );
        enableAuthAndRoles = configService.GetBool(ConfigSettings.EnablePoliciesAndRoles,
            ConfigSettings.DefaultEnableRolesAndAuth);
        forceLogin = configService.GetBool(ConfigSettings.ForceLogin);
        allowExternalRegistration = configService.GetBool(ConfigSettings.AllowExternalRegistration);
        useSmtp = configService.GetBool(ConfigSettings.UseSmtp);
        serverLogLevel = configService.Get<LoggingLevel>(ConfigSettings.LogLevel);
        writeAITagsToImages = configService.GetBool(ConfigSettings.WriteAITagsToImages);
        enableAIProcessing = configService.GetBool(ConfigSettings.EnableAIProcessing, true);
        disableObjectDetector = configService.GetBool(ConfigSettings.DisableObjectDetector);
        importSidecarKeywords = configService.GetBool(ConfigSettings.ImportSidecarKeywords);
        similarityThreshold = configService.GetInt(ConfigSettings.SimilarityThreshold, 75);

        smtpSettings.Load(configService);
        cpuSettings.Load(configService);
    }
}