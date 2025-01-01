using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.DbModels.Models;

public class SystemConfigSettings
{
    public WordpressSettings wpSettings { get; init; } = new();
    public CPULevelSettings cpuSettings { get; init; } = new();
    public SmtpSettings smtpSettings { get; init; } = new();
    public SendGridSettings sendGridSettings { get; init; } = new();
    public LoggingLevel serverLogLevel { get; set; }
    public bool importSidecarKeywords { get; set; }
    public bool useSmtp { get; set; } = true;
    public bool forceLogin { get; set; }
    public bool enableAIProcessing { get; set; } = ConfigSettings.DefaultEnableRolesAndAuth;
    public bool disableObjectDetector { get; set; }
    public bool writeAITagsToImages { get; set; }
    public bool allowExternalRegistration { get; set; }
    public bool enableAuthAndRoles { get; set; } = true;
    public bool enableImageEditing { get; set; } = false;
    public bool enableBackgroundThumbs { get; set; } = false;
    public int similarityThreshold { get; set; } = 75;

    public async Task Save(IConfigService configService)
    {
        await configService.Set(ConfigSettings.ImportSidecarKeywords, importSidecarKeywords.ToString());
        await configService.Set(ConfigSettings.LogLevel, serverLogLevel.ToString());

        await configService.Set(ConfigSettings.WordpressURL, wpSettings.URL);
        await configService.Set(ConfigSettings.WordpressUser, wpSettings.UserName);
        await configService.Set(ConfigSettings.WordpressPassword, wpSettings.Password);

        await configService.Set(ConfigSettings.WriteAITagsToImages, writeAITagsToImages.ToString());
        await configService.Set(ConfigSettings.EnableAIProcessing, enableAIProcessing.ToString());
        await configService.Set(ConfigSettings.DisableObjectDetector, disableObjectDetector.ToString());
        await configService.Set(ConfigSettings.SimilarityThreshold, similarityThreshold.ToString());
        await configService.Set(ConfigSettings.UseSmtp, useSmtp.ToString());

        await configService.Set(ConfigSettings.EnablePoliciesAndRoles, enableAuthAndRoles.ToString());
        await configService.Set(ConfigSettings.EnableImageEditing, enableImageEditing.ToString());
        await configService.Set( ConfigSettings.EnableBackgroundThumbs, enableBackgroundThumbs.ToString() );
        await configService.Set(ConfigSettings.LogLevel, serverLogLevel.ToString());

        await smtpSettings.Save(configService);
        await sendGridSettings.Save(configService);
        await cpuSettings.Save(configService);

        // If roles are disabled, disable forced Login
        var forceLoginProxy = enableAuthAndRoles && forceLogin;
        var extRegistrationProxy = enableAuthAndRoles && allowExternalRegistration;
        await configService.Set(ConfigSettings.ForceLogin, forceLoginProxy.ToString());
        await configService.Set(ConfigSettings.AllowExternalRegistration, extRegistrationProxy.ToString());
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
        sendGridSettings.Load(configService);
        cpuSettings.Load(configService);
    }
}