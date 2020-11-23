using System.Threading;

namespace Damselfly.Web.Data
{
    /// <summary>
    /// Key press conflator. Ensures we don't call methods many times for
    /// a series of keypresses when somebody is typing.
    /// For every keypress, we start a timer; if another keypress is made
    /// within that time, we cancel it and start again. If we get to the
    /// end of the timer without another key being pressed, only then do
    /// we call the callback passed into us.
    /// </summary>
    public class KeyPressConflator
    {
        private const long interval = 500;
        private Timer searchTimer;
        private TimerCallback theCallback;

        public void HandleKeyPress(TimerCallback callback)
        {
            theCallback = callback;

            if (searchTimer != null)
            {
                Logging.LogTrace("New keypress - cancelled timer");
                var oldTimer = searchTimer;
                searchTimer = null;
                oldTimer.Dispose();
            }

            searchTimer = new Timer(TimerCallback, null, interval, Timeout.Infinite);
        }

        /// <summary>
        /// Callback for the timer. If this is called, it means the user has stopped
        /// typing for half a second, so we can actually fire the callback associated
        /// with the keypress.
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallback( object state )
        {
            var oldTimer = searchTimer;
            searchTimer = null;
            if( oldTimer != null )
                oldTimer.Dispose();

            theCallback(state);
        }
    }
}
