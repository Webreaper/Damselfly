var blazorVirtualScrolling = {
    init: function(eleId, cmp) {

        var ref = document.getElementById(eleId);

        ref.scrollTop = 0;

        ref.addEventListener("scroll",
            (e) => {
                cmp.invokeMethodAsync("VirtualScrollingSetView", blazorVirtualScrolling.getScrollView(ref));

            });

        window.addEventListener("resize",
            (e) => {
                cmp.invokeMethodAsync("VirtualScrollingSetView", blazorVirtualScrolling.getScrollView(ref));

            });

        // Call to initialise our state.
        cmp.invokeMethodAsync("VirtualScrollingSetView", blazorVirtualScrolling.getScrollView(ref))
    },

    getScrollView(ref) {
        return { scrollTop: ref.scrollTop, clientHeight: ref.clientHeight };

    }

};