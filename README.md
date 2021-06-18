![Damselfly Screenshot](docs/cropped-Damselfly-Logo.webp)

# Damselfly
Damselfly is a server-based Digital Photograph Asset Management system. Damselfly is designed to manage a large, folder-based 
collection of photographs, with a particular focus on fast search and keyword-tagging. 

The user-interface and workflow is loosely based on the much-loved Picasa, with a basket to select images for export and other
types of processing. Damselfly also provides a desktop/client app which gives closer integraton with your laptop or PC, allowing
you to quickly sync a selection of images from the Damselfly basket to a local folder, for editing etc.

### Want to Support Damselfly?

Damselfly is free, open-source software. But if you find it useful, and fancy buying me a coffee or a slice of pizza, that would 
be appreciated!

<a href="https://www.buymeacoffee.com/damselfly" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/arial-green.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## Screenshots

<img src="docs/Damselfly-browsing.jpg" alt="Browsing in Damselfly" width="800"/>
<br/>
<br/>
<img src="docs/Damselfly-imageview.jpg" alt="Viewing images in Damselfly" width="800"/>
<br/>
<br/>
<img src="docs/Damselfly-theme.jpg" alt="Themes in Damselfly" width="800"/>

## Features

* Server-based deployment, with a web-based front-end UI, so the image library can be accessed via multiple 
  devices without having to copy catalogues or other DBs to local device storage.   
* Support for most impage formats including JPG, PNG, Webp, BMP and DNG (RAW) files (HEIC coming in future).
* Focus on extremely fast performance - searching a 500,000-image catalogue returns results in less than a second.
* Full-text search with multi-phrase partial-word searches
* Fast keyword tagging workflow with non-destructive EXIF data updates (using ExifTool) - so JPEGs are not re-encoded 
  when keyword-tagged
* Completely automated background indexing of images, so that the collection is automatically and quickly updated 
  when new images are added or updated
* Background thumbnail generation
* AI / Computer vision image recognition:
    * Facial detection
    * Object detection and recognition
    * Image Classification
    * Facial Recognition (requires a [free Azure Face Services account](https://azure.microsoft.com/free/cognitive-services/))
* Selection basket for collecting images from search results to save locally and work within Digikam/PhotoShop/etc.
* Download/export processing to watermark images ready for social media, or sending via Email etc.
* Runs on Windows, Linux and OSX, and in Docker.
* Electron.Net Desktop Client for hosted site to allow closer native integration with client OS
    * Desktop Client versions for MacOS (universal), Windows and Linux 
    * Synchronise images from server basket select to local filesystem for editing
    * Other integrations coming in future
* Direct upload to Wordpress 
* Persistable named basket selections
* Themes
* Built with Microsoft .Net 6, so runs cross-platform on MacOS, Linux and Windows. 

## Planned Features/Enhancements

* Image de-duplication (in progress)
* Currently Damselfly has no concept of user identity, so there are no user-specific sessions (for things 
  like selection, baskets and access control). 
* Direct upload to Social media platforms, Google Drive, etc.
* Support for more image formats (e.g., HEIC).
* Direct sharing to social media (Twitter, Facebook etc)
* Support for selection and upload to Alamy Stock Image photo service
* Simple non-destructive editing/manipulation - cropping, colour adjustment etc
* Synchronisation of local images to the server
* If you have ideas for other features - let me know by raising an issue!

---
## Installation

The easiest way to install Damselfly is via Docker. Mount your photos folder as the /pictures volume, and a config 
folder (in which the database, thumbnails etc will be stored), and you're ready to go.

### Docker Command:
```
docker run \
  --name damselfly \
  --restart unless-stopped \
  -v /volume1/dockerdata/damselfly:/config \
  -v /volume1/photo:/pictures \
  -v /volume1/dockerdata/damselfly/thumbs:/thumbs \
  -p 6363:6363 \
  -d \
  webreaper/damselfly
```

**_Note:_** If you're running on a Synology NAS, and have the Synology indexing/thumbnails enabled, you can specify `-e SYNO_THUMBS=true` which will make Damselfly use the same `@eaDir` folder structure as Synology's indexing system, which means that thumbnails already generated will be used by Damselfly (and Damselfly's thumbnails will be used by Synology Photo Station).


### Docker-Compose: 
```
 damselfly: 
        container_name: damselfly
        image: webreaper/damselfly
        ports:
            - 6363:6363/tcp
        volumes:
            - /volume1/dockerdata/damselfly:/config
            - /volume1/dockerdata/damselfly/thumbs:/thumbs
            - /volume1/photo:/pictures 
        restart: unless-stopped
```

The default port is 6363. The /pictures volume is mapped to the root directory of your photographs. By 

Note that in order to set up inotify watchers on your folders, Damselfly will increase the number of inotify instances as follows:

```
echo fs.inotify.max_user_instances=524288 | sudo tee -a /etc/sysctl.conf && sudo sysctl -p
```

Other options:
`SYNO_THUMBS=True` - Tells Damselfly to use existing Synology thumbnails (which are generated by DSM's indexing process) and to generate new thumbs in the same format.

---

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

### How should we use Damselfly? What's the workflow?

The photos live in the library on the server, but whenever you want to work on a picture (e.g., in Photoshop, Digikam or your editing tool of 
choice) you use the Damselfly Deskop app to add the images to the basket, and choose Download => Save Locally, to sync the pictures across 
the network to your local folder. Once you've finished editing, copy them back to the server (a future feature enhancement will let Damselfly 
do this for you) where the server will re-index them to pick up your changes.

### Suggested workflow.

1. Images are copied onto a laptop for initial sorting, quality checks, and IPTC tagging using Picasa or Digikam
2. [Rclone](www.rclone.org) script syncs the new images across the LAN to the network share
3. Damselfly automatically picks up the new images and indexes them (and generates thumbnails) within 30 minutes
4. Images are now searchable in Damselfly and can be added to the Damselfly 'basket' with a single click
5. Images in the basket can be copied back to the desktop/laptop for local editing in Lightroom/On1/Digikam/etc.
   * Use the Damselfly Desktop client to write the files directly to the local filesystem in the same structure as on the server.
   * Export to a zip file to download and extract into the local image folder for additional editing
6. Re-sync using RClone to push the images back to the collection [Future enhancement: Damselfly Desktop App will do this for you]

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

## How does Damselfly's Image/Object/Face Recognition Work?

The latest version of Damselfly includes machine-learning functionality to find objects and faces in your images, and tag 
them. If you're interested in the technical design/implementation of this feature, there's an article about it 
[here](https://damselfly.info/face-recognition-in-net-the-good-the-bad-and-the-ugly/). 

Faces and objects that are recognised will be displayed as special tags, alongside normal keyword-tags, when browsing the
images. You can distinguish them by the different icon (a lightbulb for objects, and a head/shoulders for faces - you can
also hold your mouse over the icon for a description). 

<img src="docs/Damselfly-AI.jpg" alt="Image Recognition in Damselfly" width="800"/>

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

---

## Background/FAQ

Some common questions/answers.

### Do I have to run it in Docker?

No, you can run it standalone. For most releases I'll provide docker images along with zip/tar files for the server and 
Desktop apps, for MacOS, Windows and Linux.

### Why 'Damselfly'?

Etymology of the name: DAM-_sel_-fly - **D**igital **A**sset **M**anagement that flies.

### What is the Damselfly Architecture?

Damselfly is written using C#/.Net 6 and Blazor Server. The data model and DB access is using Entity Framework Core. Currently the server supports Sqlite, but a future enhancement may be to add support for PostGres, MySql or MariaDB.

### How do I set up the Wordpress Integration?

Damselfly allows direct uploads of photographs to the media library of a Wordpress Blog. To enable this feature, you must configure your Wordpress site to support JWT authentication. For more details see [JWT Authentication for WP REST API](https://wordpress.org/plugins/jwt-authentication-for-wp-rest-api/).

To enable this option you’ll need to edit your .htaccess file adding the following:

    RewriteEngine on
    RewriteCond %{HTTP:Authorization} ^(.*)
    RewriteRule ^(.*) - [E=HTTP_AUTHORIZATION:%1]
    SetEnvIf Authorization "(.*)" HTTP_AUTHORIZATION=$1
    
The JWT needs a secret key to sign the token this secret key must be unique and never revealed. To add the secret key edit your wp-config.php file and add a new constant called JWT_AUTH_SECRET_KEY

    define('JWT_AUTH_SECRET_KEY', 'your-top-secret-key');

To enable the CORs Support edit your wp-config.php file and add a new constant called JWT_AUTH_CORS_ENABLE

    define('JWT_AUTH_CORS_ENABLE', true);

You can use a string from [here](https://api.wordpress.org/secret-key/1.1/salt/).

Once you have the site configured:

1. Install the [Wordpress JWT Authentication for WP REST API](https://wordpress.org/plugins/jwt-authentication-for-wp-rest-api/) 
plugin.
2. Use the config page in Damselfly to set the website URL, username and password. I recommend setting up a dedicated user account 
for Damselfly to use.

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

--- 

## Contributing to Damselfly

I am a professional developer, but Damselfly is a side-project, written in my spare time. I'm also not a web designer or CSS expert (by any means). If you'd like to contribute to Damselfly with features, enhancements, or with some proper shiny design/layout enhancements, please submit a PR!

## Building Damselfly

Damselfy is set up with GitHub Actions, so has full CI/CD. If you want to build it yourself locally, it's simple - just clone the repo, and then run `sh makeall.sh`. The default/assumed platform is 'mac' - if you want to build for Linux or Windows, use `makeall.sh linux` or `makeall.sh windows` respectively. This script will: 

* Bump the patch version
* Build the Electron Desktop apps for all 3 platforms (if you're on MacOS)
* Build the server project for the platform you specify
* Run a docker build for the Alpine/linux platform.

## Thanks and Credits

* Microsoft [Blazor.Net](https://blazor.net) for allowing me to avoid writing Javascript. ;)
* [SkiaSharp](https://github.com/mono/SkiaSharp) Fast library for Thumbnail generation
* [SixLabors ImageSharp](https://github.com/SixLabors/ImageSharp) Portable library for Thumbnail generation
* Drew Noakes' [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet), for IPTC and other image meta-data indexing
* IPTC Tag management using [ExifTool](https://exiftool.org/) by Phil Harvey
* Face recognition by [Accord.Net](http://accord-framework.net), and [Azure Cognitive Services](https://azure.microsoft.com/en-gb/services/cognitive-services/face/)
* Icons by [Font Awesome](https://fontawesome.com/)
* Chris Sainty for [Blazored](https://github.com/Blazored) Modal and Typeahead, and all his excellent info on Blazor
* [Serilog.Net](https://serilog.net/) for logging
* Wisne for [Infinite Scroll](https://github.com/wisne/InfiniteScroll-BlazorServer) inspiration
