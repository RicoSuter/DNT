# TinyPng for .Net

| Platform | Status|
|---------|-------|
|Windows  | [![Build status](https://img.shields.io/appveyor/ci/soda-digital/tinypng.svg?maxAge=2000)](https://ci.appveyor.com/project/Soda-Digital/tinypng) |
|Linux | [![Build Status](https://img.shields.io/travis/ctolkien/TinyPNG.svg?maxAge=2000)](https://travis-ci.org/ctolkien/TinyPNG) |

[![codecov](https://codecov.io/gh/ctolkien/TinyPNG/branch/master/graph/badge.svg)](https://codecov.io/gh/ctolkien/TinyPNG)
![Version](https://img.shields.io/nuget/v/tinypng.svg?maxAge=2000)
[![license](https://img.shields.io/github/license/ctolkien/TinyPNG.svg?maxAge=2592000)]()


This is a .NET wrapper around the [TinyPNG.com](http://tinypng.com) image compression service. This is not an official TinyPNG.com product.

* Supports .Net Core and full .Net Framework
* Non-blocking async turtles all the way down
* `Byte[]`, `Stream` and `File` API's available

## Installation

Install via Nuget

```
    Install-Package TinyPNG
```

Install via `dotnet`

```
    dotnet add package TinyPNG
```

## Quickstart
```csharp
using (var png = new TinyPngClient("yourSecretApiKey")) 
{
    var result = await png.Compress("cat.jpg");
    
    //URL to your compressed version
    result.Output.Url;
}
```

## Upgrading from V2

The API has changed from V2, primarily you no longer need to await each individual
step of using the TinyPNG api, you can now chain appropriate calls together as
the extension methods now operate on `Task<T>`.


## Compressing Images

```csharp
//create an instance of the TinyPngClient
using (var png = new TinyPngClient("yourSecretApiKey")) 
{
    //Create a task to compress an image.
    //this gives you the information about your image as stored by TinyPNG
    //they don't give you the actual bits (as you may want to chain this with a resize
    //operation without caring for the originally sized image).
    var compressImageTask = png.Compress("pathToFile or byte array or stream");

    //If you want to actually save this compressed image off
    //it will need to be downloaded 
    var compressedImage = await compressImageTask.Download();

    //you can then get the bytes
    var bytes = await compressedImage.GetImageByteData();

    //get a stream instead
    var stream  = await compressedImage.GetImageStreamData()

    //or just save to disk
    await compressedImage.SaveImageToDisk("pathToSaveImage");

    //Putting it all together
    await png.Compress("path")
        .Download()
        .SaveImageToDisk("savedPath");

}
```

Further details about the result of the compression are also available on the `Input` and `Output` properties of a `Compress` operation. Some examples:
```csharp

    var result = await png.Compress("pathToFile or byte array or stream");

    //old size
    result.Input.Size;
    
    //new size
    result.Output.Size;

    //URL of the compressed Image
    result.Output.Url; 

```

## Resizing Images

```csharp
using (var png = new TinyPngClient("yourSecretApiKey")) 
{
    var compressImageTask = png.Compress("pathToFile or byte array or stream");
    
    var resizedImageTask = compressImageTask.Resize(width, height);

    await resizedImageTask.SaveImageToDisk("pathToSaveImage");

    //altogether now....
    await png.Compress("pathToFile")
        .Resize(width, height)
        .SaveImageToDisk("pathToSaveImage");
}

```

### Resize Operations

There are certain combinations when specifying resize options which aren't compatible with
TinyPNG. We also include strongly typed resize operations, 
depending on the type of resize you want to do. 

```csharp
using (var png = new TinyPngClient("yourSecretApiKey")) 
{
    var compressTask = png.Compress("pathToFile or byte array or stream");
    
    await compressTask.Resize(new ScaleWidthResizeOperation(width));
    await compressTask.Resize(new ScaleHeightResizeOperation(width));
    await compressTask.Resize(new FitResizeOperation(width, height));
    await compressTask.Resize(new CoverResizeOperation(width, height));
}

```

The same `Byte[]`, `Stream` and `File` path API's are available from the result of the `Resize()` method.

## Amazon S3 Storage

The result of any compress operation can be stored directly on to Amazon S3 storage. I'd strongly recommend referring to [TinyPNG.com's documentation](https://tinypng.com/developers/reference) with regard to how to configure
the appropriate S3 access.

If you're going to be storing images for most requests onto S3, then you can pass in an `AmazonS3Configuration` object to the constructor.

```csharp
using (var png = new TinyPngClient("yourSecretApiKey", 
    new AmazonS3Configuration("awsAccessKeyId", "awsSecretAccessKey", "bucket", "region"))) 
{
    var compressedCat = await png.Compress("cat.jpg");
    var s3Uri = await png.SaveCompressedImageToAmazonS3(compressedCat, "file-name.png");

    //if you'd like to override the particular bucket or region
    //an image is being stored to from what is specified in the AmazonS3Configuration:
    var s3UriInNewSpot = await png.SaveCompressedImageToAmazonS3(
        compressedCat, 
        "file-name.png", 
        bucketOverride: "different-bucket", 
        regionOverride: "different-region");

}

```

You can also pass a `AmazonS3Configuration` object directly into calls to `SaveCompressedImageToAmazonS3`

```csharp
using (var png = new TinyPngClient("yourSecretApiKey")) 
{
    var compressedCat = await png.Compress("cat.jpg");
    var s3Uri = await png.SaveCompressedImageToAmazonS3(compressedCat, 
        new AmazonS3Configuration(
            "awsAccessKeyId", 
            "awsSecretAccessKey", 
            "bucket", 
            "region"), "file-name.png");
}

```


## Compression Count

You can get a read on the number of compression operations you've performed by inspecting the `CompressionCount` property
on the result of any operation you've performed. This is useful for keeping tabs on your API usage.

```csharp
var compressedCat = await png.Compress("cat.jpg");
compressedCat.CompressionCount; // = 5
```
