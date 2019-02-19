# cardboard-webvr

Generate a WebVR slideshow from a set of Cardboard Camera photos

## Getting Started

### Prerequisites

- Some photos taken with [Google Cardboard Camera](https://play.google.com/store/apps/details?id=com.google.vr.cyclops&hl=en_US)
- [.NET Core 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1) installed

### Prepare Your Photos
- Copy your Cardboard photos to an empty folder.
- The folder should contain no other files.
- Optional: Give the photos meaningful file names or add a metadata title to the photos. This text will appear in the generated VR app.

### Run the project

Get the code
```sh
$ git clone https://github.com/matthewjustice/cardboard-webvr.git
```

Change directories

```
$ cd src/CardboardWebVR
```

Run the application, specifying the full input folder path and output folder path.

```sh
$ dotnet run [inputFolder] [outputFolder]
```

This will generate a WebVR application in your output folder.

### View WebVR Content

To view the WebVR content you generated, locally serve the content of the output folder with a web server. I recommend [live-server](https://www.npmjs.com/package/live-server). Open a web browser and view the locally-served site.

Once you are satisfied with the content, you can host it on the public web if you wish to share it with other. It is static content so any static host will work, such as [surge.sh](https://surge.sh/) or [Netlify](https://www.netlify.com/).

## Built With
- [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/)
- [Metadata Extractor](https://github.com/drewnoakes/metadata-extractor-dotnet)
- [A-Frame](https://aframe.io/) - Framework for WebVR
- [aframe-stereo-component](https://github.com/oscarmarinmiro/aframe-stereo-component) - Stereo image support in a-frame

## Authors

- **Matthew Justice** [matthewjustice](https://github.com/matthewjustice)

See also the list of [contributors](https://github.com/matthewjustice/pumpkinpi/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
