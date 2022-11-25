window.ScrollMonitor =
    {
        Init: function (scrollAreaID, DotNetRef, initialScrollPos) {
            var scrollArea = document.getElementById(scrollAreaID);

            function onScroll()
            {
                DotNetRef.invokeMethodAsync("HandleScroll", scrollArea.scrollTop);
            }

            if (scrollArea !== null)
            {
                scrollArea.addEventListener('scroll', onScroll);
                window.addEventListener('resize', onScroll);

                // For some reason this only works if it's executed async 
                // Probably something to do with the ordering of the element
                // creation in in the DOM
                setTimeout(function () {
                    scrollArea.scrollTop = initialScrollPos;
                }, 1);

            }
        }
    }