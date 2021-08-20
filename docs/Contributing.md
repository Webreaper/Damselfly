# Contributing to Damselfly

[Return to Readme](../README.md)

## My Background

I am a professional developer, but Damselfly is a side-project, written in my spare time. I'm also not a 
web designer or CSS expert (by any means). If you'd like to contribute to Damselfly with features, 
enhancements, or with some proper shiny design/layout enhancements, please submit a PR!

## Building Damselfly

Damselfy is set up with GitHub Actions, so has full CI/CD. If you want to build it yourself locally, 
it's simple - just clone the repo, and then run `sh makeall.sh`. The default/assumed platform is 'mac' 
- if you want to build for Linux or Windows, use `makeall.sh linux` or `makeall.sh windows` respectively. 
This script will: 

* Bump the patch version
* Build the Electron Desktop apps for all 3 platforms (if you're on MacOS)
* Build the server project for the platform you specify
* Run a docker build for the Alpine/linux platform.
