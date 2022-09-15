window.ScrollMonitor =
    {
        Init: function (scrollAreaID, DotNetRef, initialScrollPos) {
            var scrollArea = document.getElementById(scrollAreaID);

            if (scrollArea !== null)
                scrollArea.scrollTop = initialScrollPos;

            function onScroll() {
                if (scrollArea === null) {
                    scrollArea = document.getElementById(scrollAreaID);
                    scrollArea.addEventListener('scroll', onScroll);
                }

                DotNetRef.invokeMethodAsync("HandleScroll", scrollArea.scrollTop);
            }

            window.addEventListener('resize', onScroll);
        }
    }