NIGHTBORNE — SOURCE-LOCKED ROBOTIC STYLE CORRECTION

The native 1840 x 400 sprite sheet has the exact source 23 x 5 grid and 80 x 80 transparent cells.
Original poses, silhouette, purple sword, slash, flashes and death particles are preserved byte-for-byte wherever possible.
Only original opaque, dark blue character-surface pixels were recolored to a small, flat steel-gray palette.
NOT a new generated sheet or reduced/blurred version of the high-resolution robotic redesign.

In Unity: import PNG as Sprite (Multiple), Pixels Per Unit matching your game's existing scale,
Filter Mode Point, Compression None, Sprite Editor -> Grid By Cell Size -> 80 x 80 px.
PNG has transparent background; GIFs intentionally keep the original teal preview background.

Original source timing/frame counts: idle 9 @ 90ms, run 6 @ 80ms, attack 12 @ 70ms,
hurt 5 @ 70ms, death 23 @ 70ms.

The source frames did not provide a six-frame single-swing story: the 12-frame attack
sequence and its existing effect are preserved as supplied, not reordered or reanimated.
For a totally new attack animation, hand-drawn silhouette/pose edits would be needed.

See Validation_Report.json for measurable image preservation checks.
