import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

W, H = 1260, 1000

def create_cover():
    # 1. Base canvas: Rich dark altar background
    canvas = Image.new("RGBA", (W, H), (24, 20, 18, 255))
    
    # Subtle parchment texture
    parchment = Image.open("Assets/_Project/Art/Textures/parchment_tile.png").convert("RGBA")
    tile_w, tile_h = parchment.size
    parchment_layer = Image.new("RGBA", (W, H))
    for x in range(0, W, tile_w):
        for y in range(0, H, tile_h):
            parchment_layer.paste(parchment, (x, y))
            
    darkener = Image.new("RGBA", (W, H), (20, 17, 15, 215))
    parchment_layer = Image.alpha_composite(parchment_layer, darkener)
    canvas = Image.alpha_composite(canvas, parchment_layer)
    draw = ImageDraw.Draw(canvas)
    
    # 2. Outer gold/antique frame
    border_inset = 36
    draw.rounded_rectangle([border_inset, border_inset, W - border_inset, H - border_inset], radius=24, outline=(190, 145, 65, 120), width=4)
    draw.rounded_rectangle([border_inset + 8, border_inset + 8, W - border_inset - 8, H - border_inset - 8], radius=18, outline=(190, 145, 65, 40), width=1)
    
    # 3. Center Token (The rounded die / altar piece)
    token_size = 460
    token_cx = W // 2
    token_cy = H // 2 - 40
    token_x0 = token_cx - token_size // 2
    token_y0 = token_cy - token_size // 2
    token_x1 = token_cx + token_size // 2
    token_y1 = token_cy + token_size // 2
    
    # Drop shadow
    shadow_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow_img)
    shadow_draw.rounded_rectangle([token_x0 + 4, token_y0 + 18, token_x1 + 4, token_y1 + 18], radius=72, fill=(0, 0, 0, 160))
    shadow_img = shadow_img.filter(ImageFilter.GaussianBlur(24))
    canvas = Image.alpha_composite(canvas, shadow_img)
    draw = ImageDraw.Draw(canvas)
    
    # Outer wood bevel of token
    draw.rounded_rectangle([token_x0 - 10, token_y0 - 10, token_x1 + 10, token_y1 + 10], radius=78, fill=(52, 38, 26, 255), outline=(205, 160, 75, 200), width=4)
    
    # Token parchment face
    token_bg = Image.new("RGBA", (token_size, token_size), (245, 238, 220, 255))
    patch = parchment.crop((0, 0, token_size, token_size))
    token_bg = Image.blend(token_bg, patch, 0.35)
    mask = Image.new("L", (token_size, token_size), 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rounded_rectangle([0, 0, token_size, token_size], radius=68, fill=255)
    canvas.paste(token_bg, (token_x0, token_y0), mask)
    draw = ImageDraw.Draw(canvas)
    
    # Inner border line inside token
    draw.rounded_rectangle([token_x0 + 12, token_y0 + 12, token_x1 - 12, token_y1 - 12], radius=58, outline=(180, 135, 55, 100), width=2)
    
    # 4. Classic Simple Hammer (From original game asset icon_thor_hammer.png)
    hammer_raw = Image.open("Assets/_Project/Art/Placeholder/icon_thor_hammer.png").convert("RGBA")
    target_hammer_size = 310
    hammer_scaled = hammer_raw.resize((target_hammer_size, target_hammer_size), Image.Resampling.LANCZOS)
    
    # Solid dark carved ink / bronze color
    hammer_color = (45, 34, 25, 255)
    hammer_tinted = Image.new("RGBA", hammer_scaled.size, hammer_color)
    hammer_tinted.putalpha(hammer_scaled.split()[3])
    
    hx = token_cx - target_hammer_size // 2
    hy = token_cy - target_hammer_size // 2 + 4
    
    # Drop shadow under hammer
    h_shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    h_shadow.paste(hammer_tinted, (hx, hy + 4), hammer_tinted)
    h_shadow = h_shadow.filter(ImageFilter.GaussianBlur(6))
    canvas = Image.alpha_composite(canvas, h_shadow)
    canvas.paste(hammer_tinted, (hx, hy), hammer_tinted)
    
    # 5. Zeus Lightning Bolt with glowing white core effect!
    bolt_raw = Image.open("Assets/_Project/Art/Placeholder/icon_zeus_bolt.png").convert("RGBA")
    target_bolt_w = 175
    target_bolt_h = 320
    bolt_scaled = bolt_raw.resize((target_bolt_w, target_bolt_h), Image.Resampling.LANCZOS)
    bolt_rotated = bolt_scaled.rotate(-14, resample=Image.Resampling.BICUBIC, expand=True)
    
    # Outer yellow/gold electric bolt
    bolt_gold = Image.new("RGBA", bolt_rotated.size, (255, 185, 30, 255))
    bolt_gold.putalpha(bolt_rotated.split()[3])
    
    # Inner white-hot electric bolt (smaller ray centered inside)
    core_w = int(target_bolt_w * 0.58)
    core_h = int(target_bolt_h * 0.94)
    bolt_core_scaled = bolt_raw.resize((core_w, core_h), Image.Resampling.LANCZOS)
    bolt_core_rot = bolt_core_scaled.rotate(-14, resample=Image.Resampling.BICUBIC, expand=True)
    bolt_core = Image.new("RGBA", bolt_core_rot.size, (255, 253, 238, 255)) # brilliant white-hot core
    bolt_core.putalpha(bolt_core_rot.split()[3])
    
    # Positioning
    bx = token_cx - bolt_gold.width // 2 + 10
    by = token_cy - bolt_gold.height // 2 - 5
    
    # Clean parchment stroke around outer gold bolt so it pops off the hammer
    bolt_mask = bolt_gold.split()[3]
    expanded_mask = bolt_mask.filter(ImageFilter.MaxFilter(9))
    stroke = Image.new("RGBA", bolt_gold.size, (245, 238, 220, 255))
    stroke.putalpha(expanded_mask)
    
    # Paste stroke (clean separation)
    canvas.paste(stroke, (bx, by), stroke)
    
    # Paste outer yellow/gold bolt
    canvas.paste(bolt_gold, (bx, by), bolt_gold)
    
    # Paste centered inner white ray
    cx_offset = (bolt_gold.width - bolt_core.width) // 2
    cy_offset = (bolt_gold.height - bolt_core.height) // 2
    canvas.paste(bolt_core, (bx + cx_offset, by + cy_offset), bolt_core)
    
    draw = ImageDraw.Draw(canvas)
    
    # 6. Typography: "GODBOARD" in Cinzel
    font_title = ImageFont.truetype("Assets/_Project/Fonts/Cinzel.ttf", 86)
    font_sub = ImageFont.truetype("Assets/_Project/Fonts/Cinzel.ttf", 26)
    
    title_text = "G O D B O A R D"
    sub_text = "THE DIE IS THE PANTHEON"
    
    title_bbox = draw.textbbox((0, 0), title_text, font=font_title)
    title_w = title_bbox[2] - title_bbox[0]
    title_x = (W - title_w) // 2
    title_y = token_y1 + 44
    
    # Title with subtle soft shadow & warm gold fill
    draw.text((title_x + 2, title_y + 3), title_text, font=font_title, fill=(10, 8, 6, 220))
    draw.text((title_x, title_y), title_text, font=font_title, fill=(235, 195, 115, 255))
    
    # Subtitle
    sub_bbox = draw.textbbox((0, 0), sub_text, font=font_sub)
    sub_w = sub_bbox[2] - sub_bbox[0]
    sub_x = (W - sub_w) // 2
    sub_y = title_y + 104
    
    draw.text((sub_x, sub_y), sub_text, font=font_sub, fill=(175, 145, 105, 220))
    dot_y = sub_y + 12
    draw.ellipse([sub_x - 30, dot_y - 3, sub_x - 24, dot_y + 3], fill=(190, 150, 75, 180))
    draw.ellipse([sub_x + sub_w + 24, dot_y - 3, sub_x + sub_w + 30, dot_y + 3], fill=(190, 150, 75, 180))

    # Downsample cleanly to recommended 630x500
    canvas_630 = canvas.resize((630, 500), Image.Resampling.LANCZOS)
    
    os.makedirs("Docs", exist_ok=True)
    canvas_630.save("Docs/godboard_cover_630x500.png", "PNG", optimize=True)
    canvas_630.save("Docs/godboard_cover_minimal_630x500.png", "PNG", optimize=True)
    canvas.save("Docs/godboard_cover_minimal_1260x1000.png", "PNG", optimize=True)
    canvas_630.save("/Users/Damid/.gemini/antigravity/brain/9c022d9c-9858-4235-93be-0c8f3b7617cf/godboard_cover_630x500.png", "PNG", optimize=True)
    canvas_630.save("/Users/Damid/.gemini/antigravity/brain/9c022d9c-9858-4235-93be-0c8f3b7617cf/godboard_cover_minimal_630x500.png", "PNG", optimize=True)
    print("Done! Restored simple hammer with dual-layer white-core lightning bolt.")

if __name__ == "__main__":
    create_cover()
