function imageZoom(imgID, srcUrl, zoomDivId, zoomPercentage) {
    var img, result, scale, aspect;
    scale = zoomPercentage / 100;
    img = document.getElementById(imgID);
    if (img === null)
        return;

    aspect = img.naturalHeight / img.naturalWidth;
    result = document.getElementById(zoomDivId);

    if (result == undefined)
        console.log("No zoom pane: " + zoomDivId);

    result.style.backgroundImage = "url('" + srcUrl + "')";
    img.addEventListener("mousemove", moveLens);
    img.addEventListener("touchmove", moveLens);
    result.addEventListener("mousemove", moveLens);
    result.addEventListener("touchmove", moveLens);
    result.addEventListener("mouseleave", moveLens);
    function moveLens(e) {
        var pos, x, y;
        /* Prevent any other actions that may occur when moving over the image */
        e.preventDefault();
        /* Get the cursor's x and y positions: */
        pos = getCursorPos(e);
        /* Calculate the position of the lens: */
        x = pos.x;
        y = pos.y;

        /* Hide the zoom when we move outside it */
        if (x > (img.width - 5) || x < 5 || y > (img.height - 5) || y < 5) {
            result.style.display = "none";
        } else
            result.style.display = "inherit";

        var bgWidth = img.naturalWidth * scale;
        var bgHeight = ((img.naturalWidth + result.clientHeight) * scale) + result.clientHeight;

        var imgAspectRatio = img.naturalHeight / img.naturalWidth;

        if (imgAspectRatio < 1) {
            bgHeight = ((img.naturalWidth - result.clientHeight) * scale);
        }

        /* Calculate the cursor percentage x/y across the lens */
        var lensXPercent = (x / result.clientWidth);
        var lensYPercent = (y / result.clientHeight);
        var imageXPos = (bgWidth - result.clientWidth) * lensXPercent * -1;
        var imageYPos = bgHeight * lensYPercent * -1;

        var newPos = imageXPos + "px " + imageYPos + "px";

        result.style.backgroundSize = bgWidth + "px"
        result.style.backgroundPosition = newPos;
    }

    function getCursorPos(e) {
        var a, x = 0, y = 0;
        e = e || window.event;
        /* Get the x and y positions of the image: */
        a = img.getBoundingClientRect();
        /* Calculate the cursor's x and y coordinates, relative to the image: */
        x = e.pageX - a.left;
        y = e.pageY - a.top;
        /* Consider any page scrolling: */
        x = x - window.pageXOffset;
        y = y - window.pageYOffset;
        return { x: x, y: y };
    }
}
