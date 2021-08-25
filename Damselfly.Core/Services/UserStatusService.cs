using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services
{
    public class UserStatusService : IStatusService
    {
        private string statusText;
        public event Action<string> OnChange;

        public UserStatusService( StatusService statusService )
        {
            statusService.OnChange += HandleGlobalStatus;
        }

        private void HandleGlobalStatus( string text )
        {
            StatusText = text;
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke(statusText);
        }

        private void SetStatus(string newText)
        {
            if (statusText != newText)
            {
                statusText = newText;
                NotifyStateChanged();
                Logging.Log($"User Status: {statusText}");
            }
        }

        public string StatusText
        {
            get { return statusText; }
            set { SetStatus(value); }
        }
    }
}