#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>
#import <CoreGraphics/CoreGraphics.h>
#import <ImageIO/ImageIO.h>

CVPixelBufferRef pixelBufferFromCGImage(CGImageRef image, int width, int height) {
    NSDictionary *options = @{
        (id)kCVPixelBufferCGImageCompatibilityKey: @YES,
        (id)kCVPixelBufferCGBitmapContextCompatibilityKey: @YES
    };
    CVPixelBufferRef pxbuffer = NULL;
    CVReturn status = CVPixelBufferCreate(kCFAllocatorDefault, width, height,
                                          kCVPixelFormatType_32BGRA,
                                          (__bridge CFDictionaryRef)options,
                                          &pxbuffer);
    if (status != kCVReturnSuccess) return NULL;
    
    CVPixelBufferLockBaseAddress(pxbuffer, 0);
    void *pxdata = CVPixelBufferGetBaseAddress(pxbuffer);
    
    CGColorSpaceRef rgbColorSpace = CGColorSpaceCreateDeviceRGB();
    CGContextRef context = CGBitmapContextCreate(pxdata, width, height, 8,
                                                 CVPixelBufferGetBytesPerRow(pxbuffer),
                                                 rgbColorSpace,
                                                 (CGBitmapInfo)kCGBitmapByteOrder32Little | kCGImageAlphaPremultipliedFirst);
    CGContextDrawImage(context, CGRectMake(0, 0, width, height), image);
    CGColorSpaceRelease(rgbColorSpace);
    CGContextRelease(context);
    CVPixelBufferUnlockBaseAddress(pxbuffer, 0);
    return pxbuffer;
}

int main() {
    @autoreleasepool {
        int width = 1280;
        int height = 720;
        int fps = 30;
        
        NSArray *storyboard = @[
            @{@"path": @"Captures/ui5_reinforce_1280x720.png", @"duration": @1.25},
            @{@"path": @"Captures/ui4_attackpath_1280x720.png", @"duration": @1.25},
            @{@"path": @"Captures/ui5_rolling_1280x720.png", @"duration": @1.00},
            @{@"path": @"Captures/ui4_combat_sacred_neutral_1280x720.png", @"duration": @1.50},
            @{@"path": @"Captures/ui4_wrath_aiming_1280x720.png", @"duration": @1.25},
            @{@"path": @"Captures/ui4_wrath_used_1280x720.png", @"duration": @1.50},
            @{@"path": @"Captures/road3_final_destinations_1280x720.png", @"duration": @1.25},
            @{@"path": @"Thumbnails_YouTube/youtube_thumbnail_1280x720_action.png", @"duration": @1.00}
        ];
        
        NSString *tempVideoPath = @"/tmp/godboard_video_temp.mp4";
        [[NSFileManager defaultManager] removeItemAtPath:tempVideoPath error:nil];
        NSURL *tempVideoURL = [NSURL fileURLWithPath:tempVideoPath];
        
        NSError *error = nil;
        AVAssetWriter *writer = [AVAssetWriter assetWriterWithURL:tempVideoURL fileType:AVFileTypeMPEG4 error:&error];
        if (!writer) {
            NSLog(@"Writer creation failed: %@", error);
            return 1;
        }
        
        NSDictionary *videoSettings = @{
            AVVideoCodecKey: AVVideoCodecTypeH264,
            AVVideoWidthKey: @(width),
            AVVideoHeightKey: @(height),
            AVVideoCompressionPropertiesKey: @{
                AVVideoAverageBitRateKey: @(2500000), // 2.5 Mbps
                AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel
            }
        };
        AVAssetWriterInput *videoInput = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeVideo outputSettings:videoSettings];
        videoInput.expectsMediaDataInRealTime = NO;
        
        NSDictionary *sourcePixelBufferAttributes = @{
            (id)kCVPixelBufferPixelFormatTypeKey: @(kCVPixelFormatType_32BGRA),
            (id)kCVPixelBufferWidthKey: @(width),
            (id)kCVPixelBufferHeightKey: @(height)
        };
        AVAssetWriterInputPixelBufferAdaptor *adaptor = [AVAssetWriterInputPixelBufferAdaptor assetWriterInputPixelBufferAdaptorWithAssetWriterInput:videoInput sourcePixelBufferAttributes:sourcePixelBufferAttributes];
        
        [writer addInput:videoInput];
        [writer startWriting];
        [writer startSessionAtSourceTime:kCMTimeZero];
        
        int currentFrame = 0;
        
        for (NSDictionary *beat in storyboard) {
            NSString *imgPath = beat[@"path"];
            double duration = [beat[@"duration"] doubleValue];
            int beatFrames = (int)round(duration * fps);
            
            CGImageSourceRef src = CGImageSourceCreateWithURL((__bridge CFURLRef)[NSURL fileURLWithPath:imgPath], NULL);
            if (!src) {
                NSLog(@"Could not load image: %@", imgPath);
                continue;
            }
            CGImageRef img = CGImageSourceCreateImageAtIndex(src, 0, NULL);
            CVPixelBufferRef buffer = pixelBufferFromCGImage(img, width, height);
            
            for (int f = 0; f < beatFrames; f++) {
                while (!videoInput.isReadyForMoreMediaData) {
                    [NSThread sleepForTimeInterval:0.005];
                }
                CMTime pts = CMTimeMake(currentFrame, fps);
                [adaptor appendPixelBuffer:buffer withPresentationTime:pts];
                currentFrame++;
            }
            
            CVPixelBufferRelease(buffer);
            CGImageRelease(img);
            CFRelease(src);
        }
        
        [videoInput markAsFinished];
        dispatch_semaphore_t sem = dispatch_semaphore_create(0);
        [writer finishWritingWithCompletionHandler:^{
            NSLog(@"Temp video finished: status=%ld", (long)writer.status);
            dispatch_semaphore_signal(sem);
        }];
        dispatch_semaphore_wait(sem, DISPATCH_TIME_FOREVER);
        
        if (writer.status != AVAssetWriterStatusCompleted) {
            NSLog(@"Video encode error: %@", writer.error);
            return 1;
        }
        
        // Merge video with audio using AVMutableComposition
        NSLog(@"Muxing video with soundtrack...");
        AVMutableComposition *composition = [AVMutableComposition composition];
        AVURLAsset *videoAsset = [AVURLAsset assetWithURL:tempVideoURL];
        AVURLAsset *audioAsset = [AVURLAsset assetWithURL:[NSURL fileURLWithPath:@"Tools/soundtrack_10s.m4a"]];
        
        AVAssetTrack *videoTrack = [[videoAsset tracksWithMediaType:AVMediaTypeVideo] firstObject];
        AVAssetTrack *audioTrack = [[audioAsset tracksWithMediaType:AVMediaTypeAudio] firstObject];
        
        CMTime duration = CMTimeMake(currentFrame, fps);
        CMTimeRange timeRange = CMTimeRangeMake(kCMTimeZero, duration);
        
        AVMutableCompositionTrack *compVideo = [composition addMutableTrackWithMediaType:AVMediaTypeVideo preferredTrackID:kCMPersistentTrackID_Invalid];
        [compVideo insertTimeRange:timeRange ofTrack:videoTrack atTime:kCMTimeZero error:&error];
        
        if (audioTrack) {
            AVMutableCompositionTrack *compAudio = [composition addMutableTrackWithMediaType:AVMediaTypeAudio preferredTrackID:kCMPersistentTrackID_Invalid];
            [compAudio insertTimeRange:timeRange ofTrack:audioTrack atTime:kCMTimeZero error:&error];
        } else {
            NSLog(@"Audio track missing in soundtrack_10s.m4a");
        }
        
        NSString *finalPath = @"godboard_gameplay_con_audio.mp4";
        [[NSFileManager defaultManager] removeItemAtPath:finalPath error:nil];
        NSURL *finalURL = [NSURL fileURLWithPath:finalPath];
        
        AVAssetExportSession *exportSession = [[AVAssetExportSession alloc] initWithAsset:composition presetName:AVAssetExportPresetHighestQuality];
        exportSession.outputURL = finalURL;
        exportSession.outputFileType = AVFileTypeMPEG4;
        exportSession.timeRange = timeRange;
        
        dispatch_semaphore_t exportSem = dispatch_semaphore_create(0);
        [exportSession exportAsynchronouslyWithCompletionHandler:^{
            NSLog(@"Export finished: status=%ld, error=%@", (long)exportSession.status, exportSession.error);
            dispatch_semaphore_signal(exportSem);
        }];
        dispatch_semaphore_wait(exportSem, DISPATCH_TIME_FOREVER);
        
        if (exportSession.status == AVAssetExportSessionStatusCompleted) {
            unsigned long long size = [[[NSFileManager defaultManager] attributesOfItemAtPath:finalPath error:nil] fileSize];
            NSLog(@"SUCCESS! Created %@ (size: %llu bytes, %.2f MB)", finalPath, size, (double)size / (1024*1024));
        } else {
            NSLog(@"Export failed: %@", exportSession.error);
            return 1;
        }
    }
    return 0;
}
