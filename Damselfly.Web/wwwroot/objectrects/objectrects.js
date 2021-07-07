function drawRect(boxId, x, y, width, height) {
    var box, theImg;
    box = document.getElementById(boxId);
    theImg = document.getElementById("theImage");

    if (box != undefined && theImg != undefined) {
        var imgPos = getImgSizeInfo( theImg );
        console.log( "RenderedWidth = " + imgPos.width + ", RenderedHeight = " + imgPos.height );

        var shortestImgSide = theImg.naturalWidth < theImage.naturalHeight ? theImg.naturalWidth : theImg.naturalHeight;
        var shortestRenderSide = imgPos.width < imgPos.height ? imgPos.width : imgPos.height;
        var ratio = shortestRenderSide / shortestImgSide;

        console.log( "Ratio: " + ratio + " x=" + x + " y=" + y );
        box.style.display = "inherit";
        box.style.left = (x * ratio) + "px";
        box.style.top = (y * ratio) + "px";
        box.style.width = (width * ratio) + "px";
        box.style.height = (height * ratio) + "px";
        console.log("Scaled Rect: x=" + box.style.left + " y=" + box.style.top);
    }
}

function getRenderedSize(contains, cWidth, cHeight, width, height, pos){
  var oRatio = width / height,
      cRatio = cWidth / cHeight;
  return function() {
    if (contains ? (oRatio > cRatio) : (oRatio < cRatio)) {
      this.width = cWidth;
      this.height = cWidth / oRatio;
    } else {
      this.width = cHeight * oRatio;
      this.height = cHeight;
    }      
    this.left = (cWidth - this.width)*(pos/100);
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

