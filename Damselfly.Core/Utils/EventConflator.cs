using System.Threading;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Utils
{
    /// <summary>
    /// Event conflator. Ensures we don't call methods many times for
    /// a series of events, such as keypresses when somebody is typing.
    /// For every event, we start a timer; if another event is handled
    /// within that time, we cancel it and start again. If we get to the
    /// end of the timer without another key being pressed, only then do
    /// we call the callback passed into us.
    /// This can be used for any type of event we want to conflate.
    /// </summary>
    public class EventConflator
    {
        private readonly long intervalMS;
        private Timer eventTimer;
        private TimerCallback theCallback;

        public EventConflator( int delayMs = 1000 )
        {
            intervalMS = delayMs;
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

            if (eventTimer != null)
            {
                Logging.LogTrace("New event - cancelled timer");
                var oldTimer = eventTimer;
                eventTimer = null;
                oldTimer.Dispose();
            }

            eventTimer = new Timer(TimerCallback, null, intervalMS, Timeout.Infinite);
        }

        /// <summary>
        /// Callback for the timer. If this is called, it means the user has stopped
        /// typing for half a second, so we can actually fire the callback associated
        /// with the keypress.
        /// </summary>
        /// <param name="state"></param>
        private void TimerCallback( object state )
        {
            var oldTimer = eventTimer;
            eventTimer = null;
            if( oldTimer != null )
                oldTimer.Dispose();
            theCallback(state);
        }
    }
}
