#!/usr/bin/env python3
"""
Border-flood-fill background removal for the UI art batch.

Every generated asset has an opaque baked-in "background" (either a fake
checkerboard meant to simulate transparency, or a flat cream/parchment fill)
instead of real alpha. This samples the image border's colors, flood-fills
outward from the border matching those colors within a tolerance, and sets
alpha=0 for anything connected to the border that matches - so background is
removed but same-colored regions fully enclosed by artwork are preserved.
"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

TOLERANCE = 28  # per-channel-ish euclidean color distance to a border reference color


def alpha_key(path_in, path_out, tolerance=TOLERANCE, corner=12, dilate=2):
    im = Image.open(path_in).convert("RGBA")
    arr = np.array(im).astype(np.int32)
    rgb = arr[:, :, :3].astype(np.float64)
    h, w = rgb.shape[:2]

    # Reference colors come ONLY from small corner blocks, not the full edge
    # ring - some assets (e.g. a banner with an ornament overhanging one
    # edge) have artwork touching the border on some sides, and including
    # those pixels as "background" reference poisons the match and eats
    # real content. Corners are reliably background across this whole batch.
    c = corner
    ring = np.concatenate([
        rgb[:c, :c, :].reshape(-1, 3),
        rgb[:c, -c:, :].reshape(-1, 3),
        rgb[-c:, :c, :].reshape(-1, 3),
        rgb[-c:, -c:, :].reshape(-1, 3),
    ])
    # Reduce to a small set of unique-ish reference colors (cluster by rounding).
    refs = np.unique((ring // 8) * 8, axis=0)

    # Mask: pixel is "background-like" if within tolerance of ANY reference color.
    match = np.zeros((h, w), dtype=bool)
    for ref in refs:
        d = np.sqrt(((rgb - ref.reshape(1, 1, 3)) ** 2).sum(axis=2))
        match |= d < tolerance

    # Dilate before labeling to bridge 1-2px anti-aliased/JPG-noise gaps in
    # the background ring (a thin dark outline right at the canvas edge can
    # otherwise sever the background into isolated, non-border-connected
    # pockets that never get removed) - erode back afterward so the final
    # mask doesn't eat into real artwork edges.
    dilated = ndimage.binary_dilation(match, iterations=dilate)
    labeled, _ = ndimage.label(dilated, structure=np.ones((3, 3)))
    border_labels = set(labeled[0, :]) | set(labeled[-1, :]) | set(labeled[:, 0]) | set(labeled[:, -1])
    border_labels.discard(0)

    bg_mask = np.isin(labeled, list(border_labels)) & match
    arr[:, :, 3] = np.where(bg_mask, 0, 255)

    out = Image.fromarray(arr.astype(np.uint8), mode="RGBA")

    # Trim fully-transparent margins so the sprite's canvas matches its content.
    bbox = out.getbbox()
    if bbox:
        out = out.crop(bbox)

    out.save(path_out)
    removed_frac = bg_mask.sum() / bg_mask.size
    return removed_frac, out.size


if __name__ == "__main__":
    src, dst = sys.argv[1], sys.argv[2]
    tol = int(sys.argv[3]) if len(sys.argv) > 3 else TOLERANCE
    frac, size = alpha_key(src, dst, tolerance=tol)
    print(f"{src} -> {dst}: removed {frac:.1%} as background, final size {size}")
