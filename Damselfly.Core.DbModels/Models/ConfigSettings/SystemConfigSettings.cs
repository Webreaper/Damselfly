using System;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace Damselfly.Core.DbModels.Models;

public class SystemConfigSettings
{
    public WordpressSettings wpSettings { get; init; } = new WordpressSettings();
    public AzureSettings azureSettings { get; init; } = new AzureSettings();
    public CPULevelSettings cpuSettings { get; init; } = new CPULevelSettings();
    public SmtpSettings smtpSettings { get; init; } = new SmtpSettings();
    public SendGridSettings sendGridSettings { get; init; } = new SendGridSettings();
    public LoggingLevel serverLogLevel { get; set; }
    public bool importSidecarKeywords { get; set; } = false;
    public bool useSmtp { get; set; } = true;
    public bool forceLogin { get; set; } = false;
    public bool enableAIProcessing { get; set; } = true;
    public bool disableObjectDetector { get; set; } = false;
    public bool writeAITagsToImages { get; set; } = false;
    public bool allowExternalRegistration { get; set; } = false;
    public bool enableAuthAndRoles { get; set; } = false;
    public int similarityThreshold { get; set; } = 75;

    public void Save( IConfigService configService )
    {
        configService.Set(ConfigSettings.ImportSidecarKeywords, importSidecarKeywords.ToString());
        configService.Set(ConfigSettings.LogLevel, serverLogLevel.ToString());

        configService.Set(ConfigSettings.WordpressURL, wpSettings.URL);
        configService.Set(ConfigSettings.WordpressUser, wpSettings.UserName);
        configService.Set(ConfigSettings.WordpressPassword, wpSettings.Password);

        configService.Set(ConfigSettings.AzureEndpoint, azureSettings.Endpoint);
        configService.Set(ConfigSettings.AzureApiKey, azureSettings.ApiKey);
        configService.Set(ConfigSettings.AzureUseFreeTier, azureSettings.UsingFreeTier.ToString());
        configService.Set(ConfigSettings.AzureDetectionType, azureSettings.DetectionType.ToString());

        configService.Set(ConfigSettings.WriteAITagsToImages, writeAITagsToImages.ToString());
        configService.Set(ConfigSettings.EnableAIProcessing, enableAIProcessing.ToString());
        configService.Set(ConfigSettings.DisableObjectDetector, disableObjectDetector.ToString());
        configService.Set(ConfigSettings.SimilarityThreshold, similarityThreshold.ToString());
        configService.Set(ConfigSettings.UseSmtp, useSmtp.ToString());

        configService.Set(ConfigSettings.EnablePoliciesAndRoles, enableAuthAndRoles.ToString());
        configService.Set(ConfigSettings.LogLevel, serverLogLevel.ToString());

        smtpSettings.Save(configService);
        sendGridSettings.Save(configService);
        cpuSettings.Save(configService);

        // If roles are disabled, disable forced Login
        var forceLoginProxy = enableAuthAndRoles && forceLogin;
        var extRegistrationProxy = enableAuthAndRoles && allowExternalRegistration;
        configService.Set(ConfigSettings.ForceLogin, forceLoginProxy.ToString());
        configService.Set(ConfigSettings.AllowExternalRegistration, extRegistrationProxy.ToString());
    }

    public void Load( IConfigService configService )
    {
        wpSettings.URL = configService.Get(ConfigSettings.WordpressURL);
        wpSettings.UserName = configService.Get(ConfigSettings.WordpressUser);
        wpSettings.Password = configService.Get(ConfigSettings.WordpressPassword);

        azureSettings.Endpoint = configService.Get(ConfigSettings.AzureEndpoint);
        azureSettings.ApiKey = configService.Get(ConfigSettings.AzureApiKey);
        azureSettings.UsingFreeTier = configService.GetBool(ConfigSettings.AzureUseFreeTier, true);
        azureSettings.DetectionType = configService.Get(ConfigSettings.AzureDetectionType, AzureDetection.Disabled);

        enableAuthAndRoles = configService.GetBool(ConfigSettings.EnablePoliciesAndRoles);
        forceLogin = configService.GetBool(ConfigSettings.ForceLogin);
        allowExternalRegistration = configService.GetBool(ConfigSettings.AllowExternalRegistration);
        useSmtp = configService.GetBool(ConfigSettings.UseSmtp);
        serverLogLevel = configService.Get<LoggingLevel>(ConfigSettings.LogLevel);
        writeAITagsToImages = configService.GetBool(ConfigSettings.WriteAITagsToImages);
        enableAIProcessing = configService.GetBool(ConfigSettings.EnableAIProcessing, true);
        disableObjectDetector = configService.GetBool(ConfigSettings.DisableObjectDetector, false);
        importSidecarKeywords = configService.GetBool(ConfigSettings.ImportSidecarKeywords);
        similarityThreshold = configService.GetInt(ConfigSettings.SimilarityThreshold, 75);

        smtpSettings.Load(configService);
        sendGridSettings.Load(configService);
        cpuSettings.Load(configService);
    }
}

