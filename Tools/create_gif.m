#import <Foundation/Foundation.h>
#import <CoreGraphics/CoreGraphics.h>
#import <ImageIO/ImageIO.h>

int main() {
    @autoreleasepool {
        NSString *outPath = @"godboard_gameplay.gif";
        NSURL *outURL = [NSURL fileURLWithPath:outPath];
        
        NSArray *frames = @[
            @{@"path": @"Captures/ui5_reinforce_1280x720.png", @"delay": @1.25},
            @{@"path": @"Captures/ui4_attackpath_1280x720.png", @"delay": @1.25},
            @{@"path": @"Captures/ui5_rolling_1280x720.png", @"delay": @1.00},
            @{@"path": @"Captures/ui4_combat_sacred_neutral_1280x720.png", @"delay": @1.50},
            @{@"path": @"Captures/ui4_wrath_aiming_1280x720.png", @"delay": @1.25},
            @{@"path": @"Captures/ui4_wrath_used_1280x720.png", @"delay": @1.50},
            @{@"path": @"Captures/road3_final_destinations_1280x720.png", @"delay": @1.25},
            @{@"path": @"Thumbnails_YouTube/youtube_thumbnail_1280x720_action.png", @"delay": @1.50}
        ];
        
        CGImageDestinationRef dest = CGImageDestinationCreateWithURL((__bridge CFURLRef)outURL, CFSTR("com.compuserve.gif"), frames.count, NULL);
        if (!dest) {
            NSLog(@"Failed to create CGImageDestination");
            return 1;
        }
        
        NSDictionary *fileProperties = @{
            (__bridge id)kCGImagePropertyGIFDictionary: @{
                (__bridge id)kCGImagePropertyGIFLoopCount: @0 // Infinite loop
            }
        };
        CGImageDestinationSetProperties(dest, (__bridge CFDictionaryRef)fileProperties);
        
        for (NSDictionary *frame in frames) {
            NSString *path = frame[@"path"];
            NSNumber *delay = frame[@"delay"];
            
            CGImageSourceRef src = CGImageSourceCreateWithURL((__bridge CFURLRef)[NSURL fileURLWithPath:path], NULL);
            if (!src) {
                NSLog(@"Could not load image at %@", path);
                continue;
            }
            CGImageRef img = CGImageSourceCreateImageAtIndex(src, 0, NULL);
            
            NSDictionary *frameProperties = @{
                (__bridge id)kCGImagePropertyGIFDictionary: @{
                    (__bridge id)kCGImagePropertyGIFDelayTime: delay,
                    (__bridge id)kCGImagePropertyGIFUnclampedDelayTime: delay
                }
            };
            
            CGImageDestinationAddImage(dest, img, (__bridge CFDictionaryRef)frameProperties);
            CGImageRelease(img);
            CFRelease(src);
        }
        
        BOOL success = CGImageDestinationFinalize(dest);
        CFRelease(dest);
        
        if (success) {
            unsigned long long size = [[[NSFileManager defaultManager] attributesOfItemAtPath:outPath error:nil] fileSize];
            NSLog(@"Successfully created %@! Size: %llu bytes (%.2f MB)", outPath, size, (double)size / (1024*1024));
        } else {
            NSLog(@"Failed to finalize GIF");
            return 1;
        }
    }
    return 0;
}
