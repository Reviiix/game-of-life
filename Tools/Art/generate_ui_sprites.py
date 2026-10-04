"""Generates the interface sprite sheet: rounded panels, a circle, the particle tile and the icon set.

Every shape is drawn white from signed distance functions with anti-aliased edges, so Images tint it
with their theme colour. Run from the repository root:

    python3 Tools/Art/generate_ui_sprites.py

The sheet is written to Assets/Art/Sprites/Interface.png. Unity reimports it automatically; the sprite
rectangles, nine-slice borders and compression live in its .meta file and are not touched by this script,
so keep each shape where SPRITES places it.
"""

import math
import os
import struct
import zlib

OUTPUT_PATH = os.path.join("Assets", "Art", "Sprites", "Interface.png")
SHEET_WIDTH = 1024
SHEET_HEIGHT = 512

# Icons are designed on a 24-unit grid, like most icon sets, and drawn at ICON_SIZE pixels.
ICON_SIZE = 192
ICON_GRID = 24.0
ICON_STROKE_RADIUS = 1.1

PANEL_SIZE = 256
PANEL_CORNER_RADIUS = 96  # also the nine-slice border, so corners stay round at any size
PARTICLE_SIZE = 128

# Name: (left, top, width, height) in sheet pixels, measured from the top-left corner.
SPRITES = {
    "RoundedPanel": (0, 0, PANEL_SIZE, PANEL_SIZE),
    "Circle": (256, 0, PANEL_SIZE, PANEL_SIZE),
    "ParticleTile": (512, 0, PARTICLE_SIZE, PARTICLE_SIZE),
    "IconPlay": (640, 0, ICON_SIZE, ICON_SIZE),
    "IconPause": (832, 0, ICON_SIZE, ICON_SIZE),
    "IconReset": (0, 256, ICON_SIZE, ICON_SIZE),
    "IconMenu": (192, 256, ICON_SIZE, ICON_SIZE),
}


# ---------- Signed distance functions (negative inside) ----------

def rounded_box(x, y, half_width, half_height, radius):
    """Distance from a point to a rounded rectangle centred on the origin."""
    offset_x = abs(x) - half_width + radius
    offset_y = abs(y) - half_height + radius
    outside = math.hypot(max(offset_x, 0.0), max(offset_y, 0.0))
    return outside + min(max(offset_x, offset_y), 0.0) - radius


def capsule(x, y, start, end, radius):
    """Distance from a point to a line segment with round ends."""
    segment_x, segment_y = end[0] - start[0], end[1] - start[1]
    point_x, point_y = x - start[0], y - start[1]
    along = max(0.0, min(1.0, (point_x * segment_x + point_y * segment_y) / (segment_x ** 2 + segment_y ** 2)))
    return math.hypot(point_x - segment_x * along, point_y - segment_y * along) - radius


def triangle(x, y, corners, rounding):
    """Distance from a point to a triangle whose corners are rounded by the given radius."""
    distance = min(capsule(x, y, corners[index], corners[(index + 1) % 3], 0.0) for index in range(3))
    inside = all_same_side(x, y, corners)
    return (-distance if inside else distance) - rounding


def all_same_side(x, y, corners):
    """Returns whether a point is inside a triangle, whichever way its corners run."""
    signs = []
    for index in range(3):
        (sx, sy), (ex, ey) = corners[index], corners[(index + 1) % 3]
        signs.append((ex - sx) * (y - sy) - (ey - sy) * (x - sx))
    return all(value >= 0 for value in signs) or all(value <= 0 for value in signs)


def shrink(corners, amount):
    """Moves each corner towards the centre so a rounded triangle keeps the original size."""
    centre_x = sum(corner[0] for corner in corners) / 3.0
    centre_y = sum(corner[1] for corner in corners) / 3.0
    shrunk = []
    for corner_x, corner_y in corners:
        length = math.hypot(corner_x - centre_x, corner_y - centre_y)
        scale = max(length - amount * 2.0, 0.0) / length
        shrunk.append((centre_x + (corner_x - centre_x) * scale, centre_y + (corner_y - centre_y) * scale))
    return shrunk


def arc(x, y, centre, radius, gap_start_degrees, gap_end_degrees, stroke_radius):
    """Distance from a point to a circular stroke with a gap between two angles, with round ends."""
    point_x, point_y = x - centre[0], y - centre[1]
    angle = math.degrees(math.atan2(point_y, point_x)) % 360.0
    if not gap_start_degrees < angle < gap_end_degrees:
        return abs(math.hypot(point_x, point_y) - radius) - stroke_radius
    ends = [math.radians(gap_start_degrees), math.radians(gap_end_degrees)]
    return min(math.hypot(point_x - radius * math.cos(end), point_y - radius * math.sin(end)) for end in ends) - stroke_radius


# ---------- Shapes, in their own coordinates ----------

def panel_distance(x, y):
    """The nine-sliced rounded panel, filling its whole square."""
    half = PANEL_SIZE / 2.0
    return rounded_box(x - half, y - half, half - 1.0, half - 1.0, PANEL_CORNER_RADIUS - 1.0)


def circle_distance(x, y):
    """A circle filling its square."""
    half = PANEL_SIZE / 2.0
    return math.hypot(x - half, y - half) - (half - 1.0)


def particle_alpha(x, y):
    """A soft-edged rounded tile matching the board's tiles, for particles."""
    half = PARTICLE_SIZE / 2.0
    softness = 6.0
    distance = rounded_box(x - half, y - half, half - 10.0, half - 10.0, 26.0)
    return max(0.0, min(1.0, 0.5 - distance / softness))


def icon_distance(name, x, y):
    """The icons, on a 24-unit grid with y pointing up."""
    unit = ICON_SIZE / ICON_GRID
    gx, gy = x / unit, ICON_GRID - y / unit
    stroke = ICON_STROKE_RADIUS
    if name == "IconPlay":
        corners = [(8.0, 5.0), (19.0, 12.0), (8.0, 19.0)]
        distance = triangle(gx, gy, shrink(corners, 1.4), 1.4)
    elif name == "IconPause":
        distance = min(rounded_box(gx - 8.25, gy - 12.0, 1.9, 6.75, 1.2), rounded_box(gx - 15.75, gy - 12.0, 1.9, 6.75, 1.2))
    elif name == "IconMenu":
        distance = min(capsule(gx, gy, (5.0, row), (19.0, row), stroke) for row in (6.5, 12.0, 17.5))
    elif name == "IconReset":
        centre = (12.0, 11.0)
        radius = 6.8
        body = arc(gx, gy, centre, radius, 90.0, 150.0, stroke)
        tip_y = centre[1] + radius
        head = triangle(gx, gy, shrink([(12.9, tip_y + 3.3), (8.6, tip_y), (12.9, tip_y - 3.3)], 0.5), 0.5)
        distance = min(body, head)
    else:
        raise ValueError(name)
    return distance * unit


# ---------- Drawing and saving ----------

def coverage(distance):
    """Turns a distance in pixels into anti-aliased coverage over one pixel."""
    return max(0.0, min(1.0, 0.5 - distance))


def alpha_for(name, x, y):
    """Returns the alpha of a sprite at a pixel centre in its own coordinates."""
    if name == "RoundedPanel":
        return coverage(panel_distance(x, y))
    if name == "Circle":
        return coverage(circle_distance(x, y))
    if name == "ParticleTile":
        return particle_alpha(x, y)
    return coverage(icon_distance(name, x, y))


def draw_sheet():
    """Draws every sprite's coverage into one alpha-only grid and returns its rows; write_png makes the pixels white."""
    alpha = [bytearray(SHEET_WIDTH) for _ in range(SHEET_HEIGHT)]
    for name, (left, top, width, height) in SPRITES.items():
        for row in range(height):
            for column in range(width):
                value = alpha_for(name, column + 0.5, row + 0.5)
                alpha[top + row][left + column] = int(round(value * 255))
    return alpha


def write_png(path, alpha_rows):
    """Saves white pixels with the given alpha as an 8-bit RGBA PNG."""
    raw = bytearray()
    for row in alpha_rows:
        raw.append(0)
        for value in row:
            raw.extend((255, 255, 255, value))

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    header = struct.pack(">IIBBBBB", SHEET_WIDTH, SHEET_HEIGHT, 8, 6, 0, 0, 0)
    with open(path, "wb") as output:
        output.write(b"\x89PNG\r\n\x1a\n")
        output.write(chunk(b"IHDR", header))
        output.write(chunk(b"IDAT", zlib.compress(bytes(raw), 9)))
        output.write(chunk(b"IEND", b""))


def main():
    """Draws the sheet and lists each sprite's rectangle in Unity's bottom-left coordinates."""
    write_png(OUTPUT_PATH, draw_sheet())
    print("Wrote", OUTPUT_PATH)
    for name, (left, top, width, height) in SPRITES.items():
        print(f"  {name}: x={left} y={SHEET_HEIGHT - top - height} w={width} h={height}")


if __name__ == "__main__":
    main()
