
# Damselfly Technical Overview   

[Return to Readme](../README.md)

- [Damselfly Technical Overview](#damselfly-technical-overview)
  - [How does Damselfly Work?](#how-does-damselfly-work)
    - [How does Damselfly index images?](#how-does-damselfly-index-images)
  - [How does Damselfly's Image/Object/Face Recognition Work?](#how-does-damselflys-imageobjectface-recognition-work)
    - [Object Recognition](#object-recognition)
    - [Face Detection](#face-detection)
    - [Face Recognition](#face-recognition)
    - [How does Damselfly manage EXIF data?](#how-does-damselfly-manage-exif-data)
    - [Why did you build Damsefly?](#why-did-you-build-damsefly)
    - [Contributing to Damselfly](#contributing-to-damselfly)

## How does Damselfly Work?

A lot of the UI/UX paradigms within Damselfly are based on the same sorts of functionality found in Google's highly popular, but now-defunct
Picasa application. I've used Picasa for over a decade, and the speed and simplicity of its management of photos, particularly around search,
simple edits, and keyword tagging/management, are unmatched anywhere else in my opinion. Unfortunately, Google decided to demise Picasa, and 
whilst it still works, it's becoming less and less practical to use (e.g., on MacOS, it runs on Catalina/Big Sur, but can't be installed because
the installer is 32-bit). So I've borrowed some of the concepts for Damselfly:

* Basket selection - images within Dasmselfly can be added to the basket by clicking the round button on each thumb. 
* Once in the basket, certain operations can be carried out, such as uploading to Wordpress in a single click, or exporting at various resolutions,
with or without a watermark.
* There's also a selection model, for quick operations on a few images in the browse screen. Click to toggle selection, and then add keywords, or
add to the baskey, using the buttons at the bottom of the browse area. Note: the selection model needs some improvements to support shift-select,
etc - these will be coming.
* Adding keywords (IPTC tags) can be done for selected images, images in the basket or the currently displayed image on the properties screen.

### How does Damselfly index images?

At startup, Damselfly will run a full index on the folder of images passed in as the only mandatory parameter (or the volume mapped to /pictures 
if you're running in Docker). The process runs as follows:

1. Damselfly will scan the entire folder tree for any images (currently JPEG, PNG, etc; HEIC and others will be added when Google's image processing
library Skia supports them). This process is normally very quick - on my Synology NAS, around half a million images can be scanned in an hour or two).
During this process, a filesystem iNotify watcher will be set up for each folder in the tree to watch for changes.
2. Next, Damselfly will then go back through all of the images, in newest (by file-modified-date) order first, and run the metadata scan. This is 
more time-consuming, but will pull in all of the metadata such as the resolution, keyword tags, other EXIF data, when the photo was taken, etc.
3. Lastly, the thumbnail generation process will run - generating the small previews used when browsing. This process is CPU-intensive, and can take
some time; for my 4TB+ collection of half a million photos, it'll take 5+ days to run on my Synology NAS - processing around 100 images per minute.
It'll also hammer the CPU while it runs, so be aware of that. 
4. Image/object/face detection will run on the thumbnails after they've been generated. Thumbnails are used (rather than original images) because
image recognition models usually work faster with lower-res images.

Once step #2 finishes, the first full-index is complete. From that point onwards, changes to the image library should be picked up almost instantly
and processed quickly; adding a new folder of photos after a day out should only take a few seconds to be indexed, and a couple of minutes for the
thumbnails to be generated.

## How does Damselfly's Image/Object/Face Recognition Work?

The latest version of Damselfly includes machine-learning functionality to find objects and faces in your images, and tag 
them. If you're interested in the technical design/implementation of this feature, there's an article about it 
[here](https://damselfly.info/face-recognition-in-net-the-good-the-bad-and-the-ugly/). 

Faces and objects that are recognised will be displayed as special tags, alongside normal keyword-tags, when browsing the
images. You can distinguish them by the different icon (a lightbulb for objects, and a head/shoulders for faces - you can
also hold your mouse over the icon for a description). 

<img src="./Damselfly-AI.jpg" alt="Image Recognition in Damselfly" width="800"/>

### Object Recognition

The initial revision of the object-recognition is high-level and simplistic, but will recognise a number of objects such as 
cats, people, cars etc. In future I hope to improve this with enhanced recognition models.

### Face Detection

Damselfly will run face detection on all images, to try and identify faces within each image. The accuracy is good,
but you may find false-positives or missed faces occasionally. Note that this functionality all runs offline/locally, and 
is quite CPU-intensive.

For thsoe with an interest in ML, the technique used to detect faces is by applying a number of 
[Haar Cascade classifiers](https://en.wikipedia.org/wiki/Haar-like_feature) with pre-trained models, to the image. Since
these cascades can be used to detect other objects, Damselfly supports the ability to add your own models, which it will
then use to detect items. To do this, map your docker container to the `/Models` folder, and put your cascades in a 
folder structure where the containing folder indicates the tag to be applied when an object is found. 

So for example, if you had a pre-trained cascade classifier that could identify butterflies, you can add it to Damselfly
by putting it in this folder:
```
   Models/butterfly/haarcascade_butterflies.xml
```
will apply the tag 'butterfly' to any object found through this particular cascade/model. By default Damselfly ships with
the main face-detection cascades from [OpenCV](https://github.com/opencv/opencv/tree/master/data/haarcascades).

### Face Recognition

To implement Facial Recognition, Damselfly uses the [Azure Face](https://azure.microsoft.com/services/cognitive-services/face/)
online Face-recognition service provided by Microsoft. Images are submitted to the service, where they undergo a very accurate
face-detection process; if faces are detected, they are then submitted for identification, to match against a known list of 
faces previously found by Damselfly. Damselfly then uses these unique Face IDs to build a database of people, and you can then 
associate a name with each identified face; enable the object/face rects using the icon in the toolbar above the image, and 
click on a face to name it.

Note that Microsoft does not store images, or any personal information about each person, other than identifer for a particular 
identifer for a unique face. No names are submitted or associated with each face. You can read more about Microsoft's Azure 
Face platform [here](https://docs.microsoft.com/en-us/azure/cognitive-services/face/overview) and the privacy policy 
[here](https://azure.microsoft.com/en-us/support/legal/cognitive-services-compliance-and-privacy/).

You can [Sign up for a free Azure Face account](https://azure.microsoft.com/free/cognitive-services/), which will allow you
to process around 10,000 face-recognition operations per month (note that transactions-per-minute are limited, but Damselfly 
will throttle them automatically). To ensure these transactions aren't wasted on pictures that do not contain faces, Damselfly
has an option in the config page to allow you to only submit images to Azure when a face or person has already been detected 
by Damselfly's offline processing.

### How does Damselfly manage EXIF data?

Adding Keywords to EXIF metadata is a critical part of the Damselfly workflow. This is done using the excellent 
[ExifTool](https://exiftool.org/) - which is the fastest, cleanest and most powerful way to manage ExifData. ExifTool
makes the data changes losslessly, guaranteeing that your images will not be re-encoded when changing data, so no 
data will be lost for images stored with lossy formats such as JPEG etc. 

When you tag images in Damselfly, the EXIF data is not written immediately, but instead the keyword changes are written to a 
'pending metadata operations' queue. Damselfly then processes this queue of pending operations in the background,
conflating multiple EXIF operations (both addition and removal of keywords, and other metadata changes) and then running 
ExifTool to apply those changes losslessly to the image files. 

This means that the fewest disk operations necessary are carried out - reducing any risk of image file corruption, and making
the process as fast as possible - all the while keeping the Damselfly UI super-fast and responsive, even if you are adding 
many tags to hundreds of image. The other advantage of doing it this way is that if you happen to restart Damselfly or have 
some other problem, the queue of pending operations can continue to be processed, guaranteeing that a tag added via the UI 
will be written to the underlying image file. 

### Why did you build Damsefly?

I wrote Damselfly mainly to cater for a personal use-case, but decided to open-source it to make it available for others to use if 
it works for them. My wife is a horticultural writer and photographer ([pumpkinbeth.com](http://www.pumpkinbeth.com)) and over the 
years has accumulated a horticultural photography library with more than half a million pictures, in excess of 4.5TB of space. 

In order to find and retrieve photographs efficiently when writing about a particular subject, all of the photos are meticulously tagged with IPTC keywords describing the content and subject matter of each image. However, finding a digital asset management system that supports that volume of images is challenging. We've considered many software packages, and each has problems:

* Lightroom
  * Pro: Excellent keyword tagging support
  * Pro: Fast when used with local images
  * Con: Catalogues are not server-based, which means that images can only be searched from one laptop or device.
  * Con: Catalogue performance is terrible with more than about 50,000 images - which means multiple catalogues, or terrible performance
  * Con: Importing new images across the LAN (when the catalogue is based on a NAS or similar) is slow.
  * Con: Imports are not incremental by date, which means that to add new photos, the entire 3TB collection must be read across the LAN
  * Con: Lightroom 6 is 32-bit only, so not supported on OSX Catalina
* Picasa
  * Pro: Simple UI and workflow
  * Pro: Very fast and efficient IPTC keyword tagging
  * Con: Doesn't support network shares properly
  * Con: Can't handle more than about 15,000 images before it starts to behave erratically
  * Con: No longer supported by Google, and 32-bit only, so no OSX Catalina support
* ON1 RAW
  * Pro: Simple UI and workflow
  * Pro: Fast cataloging/indexing of local photos
  * Pro: Not too expensive
  * Con: Slow to index across a network share
  * Con: Crashes. A lot.
* FileRun
  * Pro: Great search support
  * Con: Really designed for documents, rather than specifically for image management
  * Con: Can support server-side indexing and shared multi-device catalogues - but Windows-only
* ACDSee
  * Pro: Fast, 
  * Con: The 'non-destructive' workflow is, actually, destructive, and can easily result in loss of images.
* Digikam
  * Pro: Free/OSS
  * Pro: Excellent for working with a local collection
  * Con: Performance is terrible for collections > 50k images, whether using Sqlite or MySql/MariaDB. 
  * Con: Startup takes > 10 minutes on OSX with 100k+ images.
* Google Photos 
  * Pro: Excellent for large collections
  * Pro: Image recognition technology can help with searching
  * Con: Search ignores IPTC tags
  * Con: Expensive for > 1TB storage
* Amazon Cloud Drive
  * Pro: Excellent for large collections
  * Pro: Unlimited Storage of images included free with Prime
  * Pro: Image recognition technology can help with searching
  * Con: Search ignores IPTC tags
  * Con: Only supports Amazon's native apps. No support for _any_ third party clients.

  ### Contributing to Damselfly

  For information on how to contribute to Damselfly, [see here](./Contributing.md).