window.InfiniteScroll =
{
    Init: function (scrollAreaID, endMarkerId, DotNetRef) {
        var scrollArea = document.getElementById(scrollAreaID);

        var markerIsVisible = function ()
        {
            var endMarker = document.getElementById(endMarkerId);

            if (endMarker === null)
                return false;

            var bounding = endMarker.getBoundingClientRect();

            return (bounding.top >= 0 && bounding.left >= 0 &&
                bounding.right <= (window.innerWidth || document.documentElement.clientWidth) &&
                bounding.bottom <= (window.innerHeight || document.documentElement.clientHeight));
        }

        var markerVisibleState = null;

        function loadIfMoreVisible() {
            var visible = markerIsVisible();

            if (markerVisibleState != visible)
            {
                markerVisibleState = visible;
                if (visible)
                {
                    DotNetRef.invokeMethodAsync("LoadMoreData");
                }
            }
        }

        window.addEventListener('resize', loadIfMoreVisible);
        scrollArea.addEventListener('scroll', loadIfMoreVisible);
    }
}