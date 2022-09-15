/*
    This scales a div to the same size as the image (which itself is automatically
    scaled using content:fit). This ensures we have a div which perfectly matches
    the image, and can then render divs for objects and faces that will end up in
    the correct relative position. 
*/
function ScaleToFitImage(imgId, boxId) {
    var box, theImg;
    box = document.getElementById(boxId);
    theImg = document.getElementById(imgId);

    function recalcSize() {
        var imgPos = getImgSizeInfo(theImg);

        var leftOffset = (theImg.clientWidth - imgPos.width) / 2;
        var topOffset = (theImg.clientHeight - imgPos.height) / 2;

        box.style.width = (imgPos.width) + "px";
        box.style.height = (imgPos.height) + "px";
        box.style.left = leftOffset + "px";
        box.style.top = topOffset + "px";
        box.style.display = "inherit";
    }

    if (box != undefined && theImg != undefined) {
        recalcSize();
        theImg.onload = recalcSize;
        window.addEventListener('resize', recalcSize);
    }
}

function getRenderedSize(contains, cWidth, cHeight, width, height, pos) {
    var oRatio = width / height,
        cRatio = cWidth / cHeight;
    return function () {
        if (contains ? (oRatio > cRatio) : (oRatio < cRatio)) {
            this.width = cWidth;
            this.height = cWidth / oRatio;
        } else {
            this.width = cHeight * oRatio;
            this.height = cHeight;
        }
        this.left = (cWidth - this.width) * (pos / 100);
        this.right = this.width + this.left;
        return this;
    }.call({});
}

function getImgSizeInfo(img) {
    var pos = window.getComputedStyle(img).getPropertyValue('object-position').split(' ');
    return getRenderedSize(true,
        img.width,
        img.height,
        img.naturalWidth,
        img.naturalHeight,
        parseInt(pos[0]));
}

