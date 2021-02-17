using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Service to maintain state around the toolbars - such as whether
    /// we show the folder list or not.
    /// </summary>
    public class ViewDataService 
    {
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        public class SideBarState
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            public bool ShowFolderList { get; set; } = false;
            public bool ShowTags { get; set; } = false;
            public bool ShowBasket { get; set; } = false;
            public bool ShowExport { get; set; } = false;
            public bool ShowImageProps { get; set; } = false;
            public bool ShowLogs { get; set; } = false;

            public override bool Equals(object obj)
            {
                var other = obj as SideBarState;
                if( other != null )
                {
                    return ShowBasket == other.ShowBasket &&
                           ShowFolderList == other.ShowFolderList &&
                           ShowExport == other.ShowExport &&
                           ShowTags == other.ShowTags &&
                           ShowImageProps == other.ShowImageProps &&
                           ShowLogs == other.ShowLogs;
                }

                return false;
            }
        }
        private SideBarState sidebarState = new SideBarState();
        public event Action<SideBarState> SideBarStateChanged;

        protected void OnStateChanged(SideBarState state)
        {
            SideBarStateChanged?.Invoke(state);
        }

        public void SetSideBarState( SideBarState state )
        {
            sidebarState = state;

            if( ! state.Equals( sidebarState ))
                OnStateChanged( state );
        }

        public bool ShowFolderList { get => sidebarState.ShowFolderList; }
        public bool ShowTags { get => sidebarState.ShowFolderList; }
        public bool ShowBasket { get => sidebarState.ShowBasket; }
        public bool ShowExport { get => sidebarState.ShowExport; }
        public bool ShowImageProps { get => sidebarState.ShowImageProps; }
        public bool ShowLogs { get => sidebarState.ShowLogs; }
    }
}
