
function DoCropSelection(imgId) {
    var image = document.getElementById(imgId);

    if (image != undefined) {
        console.log("Cropping...");
        var cropper = new Cropper(image, {
            aspectRatio: NaN,
            responsive: false,
            background: false,
            rotatable: false,
            zoomable: false,
            crop(event) {
                console.log(event.detail.x);
                console.log(event.detail.y);
                console.log(event.detail.width);
                console.log(event.detail.height);
                console.log(event.detail.rotate);
                console.log(event.detail.scaleX);
                console.log(event.detail.scaleY);
            },
        });
    }
}