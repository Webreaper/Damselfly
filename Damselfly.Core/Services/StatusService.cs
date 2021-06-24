using System;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services
{
    public class StatusService
    {
        private string statusText;
        public event Action<string> OnChange;

        public StatusService()
        {
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