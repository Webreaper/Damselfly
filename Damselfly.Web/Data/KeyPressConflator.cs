using System.Threading;

namespace Damselfly.Web.Data
{
    /// <summary>
    /// Event conflator. Ensures we don't call methods many times for
    /// a series of events, such as keypresses when somebody is typing.
    /// For every event, we start a timer; if another event is handled
    /// within that time, we cancel it and start again. If we get to the
    /// end of the timer without another key being pressed, only then do
    /// we call the callback passed into us.
    /// </summary>
    public class EventConflator
    {
        private readonly long interval;
        private Timer searchTimer;
        private TimerCallback theCallback;

        public EventConflator( int delay = 500 )
        {
            interval = delay;
        }

        /// <summary>
        /// An event is pushed into the queue. We overwrite an existing
        /// event to replace it with the new one, and then start the
        /// timer. If there's an existing timer, we kill it and create
        /// a new one, starting from zero again.
        /// </summary>
        /// <param name="callback"></param>
        public void HandleEvent(TimerCallback callback)
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
