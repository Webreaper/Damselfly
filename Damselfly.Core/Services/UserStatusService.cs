using System;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services
{
    public class UserStatusService
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
            Logging.Log($"Status: {statusText}");

            OnChange?.Invoke(statusText);
        }

        public string StatusText
        {
            get { return statusText; }
            set { statusText = value; NotifyStateChanged(); }
        }
    }
}