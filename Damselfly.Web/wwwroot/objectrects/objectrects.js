/*
Percentages passed in for x/y/w/h
*/
function drawRect(boxId, x, y, width, height) {
    var box, theImg;
    box = document.getElementById(boxId);
    theImg = document.getElementById("theImage");

    if (box != undefined && theImg != undefined) {
        var imgPos = getImgSizeInfo( theImg );

        var leftOffset = (theImg.clientWidth - imgPos.width) / 2;
        var topOffset = (theImg.clientHeight - imgPos.height) / 2;
        console.log( "Offset: X=" + leftOffset + " Y=" + topOffset );

        box.style.width = (imgPos.width * width) + "px";
        box.style.height = (imgPos.height * height) + "px";
        box.style.left = ((imgPos.left * x) + leftOffset) + "px";
        box.style.top = ((imgPos.top * y) + topOffset) + "px";
        box.style.display = "inherit";

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

