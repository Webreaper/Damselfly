using Damselfly.Core.DbModels.Models.APIModels;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientUIStateService
{
    private UIClientState? clientState = null;

    public ClientUIStateService(ILogger<ClientUIStateService> logger)
    {
        _logger = logger;
    }

    private ILogger<ClientUIStateService> _logger;
    public bool IsSmallScreenDevice => clientState == null ? false : clientState.IsSmallScreenDevice;
    public bool IsPortraitDevice => clientState == null ? false : clientState.IsPortrait;
    public bool IsWideScreen => clientState == null ? false : clientState.IsWideScreen;
    public int ScreenWidth => clientState == null ? 0 : clientState.ViewportWidth;

    public UIClientState? ClientState => clientState;

    public Action<UIClientState>? OnStateChanged { get; set; }

    public void InitialiseState(UIClientState state)
    {
        clientState = state;
        _logger.LogInformation("Client UI State initialised: {state}", clientState);

        // Notify any listeners
        OnStateChanged?.Invoke(clientState);
    }

    public void ResolutionChanged(int newWidth, int newHeight)
    {
        if ( clientState != null )
        {
            var prevMobileState = clientState.IsSmallScreenDevice;
            var prevWideState = clientState.IsWideScreen;

            clientState.ViewportHeight = newHeight;
            clientState.ViewportWidth = newWidth;

            if ( prevMobileState != clientState.IsSmallScreenDevice || prevWideState != clientState.IsWideScreen )
            {
                _logger.LogInformation("Screen State Changed: {state}", clientState);

                OnStateChanged?.Invoke(clientState);
            }
        }
    }
}