function drawRect(boxId, x, y, width, height) {
    var box, theImg;
    box = document.getElementById(boxId);
    theImg = document.getElementById("theImage");

    if (box != undefined && theImg != undefined) {

        var imgPos = getImgSizeInfo( theImg );
    
        box.style.display = "inherit";
        box.style.top = y "px";
        box.style.left = x "px";
        box.style.width = width + "px";
        box.style.height = height + "px";
        console.log("rect: " + x + " x " + y);
    }
    else
        console.log("No box");
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

