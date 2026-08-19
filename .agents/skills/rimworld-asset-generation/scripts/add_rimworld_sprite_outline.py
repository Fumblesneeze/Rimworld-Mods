#!/usr/bin/env python3
"""Deterministically add and validate a RimWorld exterior sprite contour."""

from __future__ import annotations

import argparse
from collections import deque
import hashlib
import json
import math
import os
from pathlib import Path
import sys
import uuid
import xml.etree.ElementTree as ET

try:
    import PIL
    from PIL import Image
except ImportError as error:  # pragma: no cover - exercised by the wrapper's runtime failure path
    print(f"Pillow 12.2.0 is required: {error}", file=sys.stderr)
    raise SystemExit(2)


FOREGROUND_ALPHA = 96
DARK_LUMA = 80.0
MINIMUM_COMPONENT_AREA = 2
MAXIMUM_EXCLUSION_PADDING_FINAL_PIXELS = 4.0
CONTOUR_SUPERSAMPLE = 4


class UsageError(Exception):
    """The invocation or approval input is invalid."""


class ApprovalError(Exception):
    """The produced image violates its approval contract."""


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Add a bounded exterior contour without changing existing sprite pixels."
    )
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--stroke-pixels", required=True, type=int, choices=range(1, 65))
    parser.add_argument("--outline-color", default="#17130F")
    parser.add_argument("--baseline", type=Path)
    parser.add_argument("--topology-manifest", type=Path)
    parser.add_argument("--asset-id")
    parser.add_argument("--mask-input", type=Path)
    parser.add_argument("--mask-output", type=Path)
    parser.add_argument("--output-mode", choices=("table", "json"), default="table")
    return parser.parse_args()


def require_exact_runtime() -> None:
    if PIL.__version__ != "12.2.0":
        raise UsageError(
            f"Pillow 12.2.0 is required for the approved resampler; found {PIL.__version__}."
        )


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def parse_color(value: str) -> tuple[int, int, int, int]:
    if len(value) != 7 or value[0] != "#":
        raise UsageError("Outline color must use #RRGGBB syntax.")
    try:
        return (int(value[1:3], 16), int(value[3:5], 16), int(value[5:7], 16), 255)
    except ValueError as error:
        raise UsageError("Outline color must use #RRGGBB syntax.") from error


def exterior_background(alpha: list[int], width: int, height: int) -> list[bool]:
    exterior = [False] * (width * height)
    queue: deque[int] = deque()

    def admit(index: int) -> None:
        if not exterior[index] and alpha[index] < FOREGROUND_ALPHA:
            exterior[index] = True
            queue.append(index)

    for x in range(width):
        admit(x)
        admit((height - 1) * width + x)
    for y in range(height):
        admit(y * width)
        admit(y * width + width - 1)

    while queue:
        index = queue.popleft()
        x = index % width
        y = index // width
        if x > 0:
            admit(index - 1)
        if x + 1 < width:
            admit(index + 1)
        if y > 0:
            admit(index - width)
        if y + 1 < height:
            admit(index + width)
    return exterior


def squared_euclidean_distance_to_nontransparent(
    alpha: list[int], width: int, height: int
) -> list[float]:
    """Return the exact squared pixel-center distance to the nearest alpha-bearing pixel."""

    infinity = float("inf")

    def transform_1d(values: list[float]) -> list[float]:
        sites = [index for index, value in enumerate(values) if value < infinity]
        if not sites:
            return [infinity] * len(values)

        parabola = [0] * len(sites)
        boundaries = [0.0] * (len(sites) + 1)
        envelope_index = 0
        parabola[0] = sites[0]
        boundaries[0] = -infinity
        boundaries[1] = infinity

        for site in sites[1:]:
            while True:
                previous = parabola[envelope_index]
                intersection = (
                    (values[site] + site * site)
                    - (values[previous] + previous * previous)
                ) / (2.0 * (site - previous))
                if intersection > boundaries[envelope_index]:
                    break
                envelope_index -= 1
            envelope_index += 1
            parabola[envelope_index] = site
            boundaries[envelope_index] = intersection
            boundaries[envelope_index + 1] = infinity

        result = [infinity] * len(values)
        envelope_index = 0
        for coordinate in range(len(values)):
            while boundaries[envelope_index + 1] < coordinate:
                envelope_index += 1
            site = parabola[envelope_index]
            result[coordinate] = (
                (coordinate - site) * (coordinate - site) + values[site]
            )
        return result

    row_distance = [infinity] * (width * height)
    for y in range(height):
        row = [
            0.0 if alpha[y * width + x] > 0 else infinity
            for x in range(width)
        ]
        transformed = transform_1d(row)
        row_distance[y * width : (y + 1) * width] = transformed

    distance = [infinity] * (width * height)
    for x in range(width):
        column = transform_1d([row_distance[y * width + x] for y in range(height)])
        for y, value in enumerate(column):
            distance[y * width + x] = value
    return distance


def antialiased_contour_alpha(
    alpha: list[int], width: int, height: int, stroke_pixels: int
) -> list[int]:
    """Rasterize a rounded contour above source resolution and filter it back once."""

    scale = CONTOUR_SUPERSAMPLE
    high_width = width * scale
    high_height = height * scale
    high_alpha = [
        alpha[(y // scale) * width + x // scale]
        for y in range(high_height)
        for x in range(high_width)
    ]
    squared_distance = squared_euclidean_distance_to_nontransparent(
        high_alpha, high_width, high_height
    )
    radius = stroke_pixels * scale
    coverage: list[int] = []
    for distance_squared in squared_distance:
        if not math.isfinite(distance_squared):
            coverage.append(0)
            continue
        amount = min(1.0, radius + 0.5 - math.sqrt(distance_squared))
        coverage.append(max(0, round(255 * amount)))

    high_contour = Image.new("L", (high_width, high_height))
    high_contour.putdata(coverage)
    contour = high_contour.resize(
        (width, height),
        Image.Resampling.LANCZOS,
        box=(0, 0, high_width, high_height),
        reducing_gap=None,
    )
    return [value if value >= 4 else 0 for value in contour.get_flattened_data()]


def connected_components(
    selected: list[bool], width: int, height: int, connectivity: int
) -> list[tuple[int, bool]]:
    seen = [False] * (width * height)
    components: list[tuple[int, bool]] = []
    offsets = [(-1, 0), (1, 0), (0, -1), (0, 1)]
    if connectivity == 8:
        offsets += [(-1, -1), (1, -1), (-1, 1), (1, 1)]

    for start, is_selected in enumerate(selected):
        if not is_selected or seen[start]:
            continue
        seen[start] = True
        queue: deque[int] = deque([start])
        area = 0
        touches_edge = False
        while queue:
            index = queue.popleft()
            area += 1
            x = index % width
            y = index // width
            touches_edge = touches_edge or x == 0 or y == 0 or x == width - 1 or y == height - 1
            for delta_x, delta_y in offsets:
                next_x = x + delta_x
                next_y = y + delta_y
                if next_x < 0 or next_x >= width or next_y < 0 or next_y >= height:
                    continue
                next_index = next_y * width + next_x
                if selected[next_index] and not seen[next_index]:
                    seen[next_index] = True
                    queue.append(next_index)
        components.append((area, touches_edge))
    return components


def topology(image: Image.Image) -> tuple[int, int]:
    width, height = image.size
    alpha = [pixel[3] for pixel in image.get_flattened_data()]
    foreground = [value >= FOREGROUND_ALPHA for value in alpha]
    foreground_components = sum(
        1
        for area, _ in connected_components(foreground, width, height, 8)
        if area >= MINIMUM_COMPONENT_AREA
    )
    background = [not value for value in foreground]
    background_holes = sum(
        1
        for area, touches_edge in connected_components(background, width, height, 4)
        if area >= MINIMUM_COMPONENT_AREA and not touches_edge
    )
    return foreground_components, background_holes


def resize_for_approval(image: Image.Image, width: int, height: int) -> Image.Image:
    if image.size == (width, height):
        return image.copy()
    return image.resize(
        (width, height),
        Image.Resampling.LANCZOS,
        box=(0, 0, image.width, image.height),
        reducing_gap=None,
    )


def read_approval(
    manifest_path: Path | None, asset_id: str | None, input_path: Path
) -> tuple[ET.Element | None, int, int]:
    if manifest_path is None or asset_id is None:
        raise UsageError("Topology manifest and asset id are required.")
    root = ET.parse(manifest_path).getroot()
    matches = [item for item in root.findall(".//sprite") if item.get("id") == asset_id]
    if len(matches) != 1:
        raise UsageError(
            f"Expected exactly one sprite approval with id '{asset_id}', found {len(matches)}."
        )
    approval = matches[0]
    expected_hash = approval.get("preOutlineSha256")
    if expected_hash is None or expected_hash.upper() != sha256(input_path):
        raise UsageError(f"Pre-outline SHA-256 does not match approval '{asset_id}'.")
    try:
        return approval, int(approval.attrib["finalWidth"]), int(approval.attrib["finalHeight"])
    except (KeyError, ValueError) as error:
        raise UsageError(f"Approval '{asset_id}' has invalid final dimensions.") from error


def validate_approval(image: Image.Image, approval: ET.Element | None, phase: str) -> None:
    if approval is None:
        return
    try:
        expected_components = int(approval.attrib["foregroundComponents"])
        expected_holes = int(approval.attrib["backgroundHoles"])
    except (KeyError, ValueError) as error:
        raise UsageError("Approval topology counts are missing or invalid.") from error
    actual_components, actual_holes = topology(image)
    if actual_components != expected_components or actual_holes != expected_holes:
        raise ApprovalError(
            f"{phase} topology is {actual_components} components/{actual_holes} holes; "
            f"expected {expected_components}/{expected_holes}."
        )

    alpha = [pixel[3] for pixel in image.get_flattened_data()]
    for gap in approval.findall("protectedGap"):
        orientation = gap.get("orientation")
        try:
            fixed = int(gap.attrib["fixedCoordinate"])
            start = int(gap.attrib["start"])
            end = int(gap.attrib["end"])
            minimum = int(gap.attrib["minimumTransparentRunPixels"])
        except (KeyError, ValueError) as error:
            raise UsageError("Protected gap coordinates are missing or invalid.") from error
        if start > end or minimum < 1:
            raise UsageError("Protected gap range or minimum run is invalid.")
        if orientation == "horizontal":
            if fixed < 0 or fixed >= image.height or start < 0 or end >= image.width:
                raise UsageError("Horizontal protected gap is outside the final canvas.")
            values = [alpha[fixed * image.width + x] for x in range(start, end + 1)]
        elif orientation == "vertical":
            if fixed < 0 or fixed >= image.width or start < 0 or end >= image.height:
                raise UsageError("Vertical protected gap is outside the final canvas.")
            values = [alpha[y * image.width + fixed] for y in range(start, end + 1)]
        else:
            raise UsageError("Protected gap orientation must be horizontal or vertical.")
        longest = 0
        current = 0
        for value in values:
            if value < FOREGROUND_ALPHA:
                current += 1
                longest = max(longest, current)
            else:
                current = 0
        if longest < minimum:
            raise ApprovalError(
                f"{phase} protected {orientation} gap retains {longest} transparent pixels; "
                f"expected at least {minimum}."
            )


def source_contour_exclusions(
    approval: ET.Element | None,
    width: int,
    height: int,
    final_width: int,
    final_height: int,
) -> list[bool]:
    excluded = [False] * (width * height)
    if approval is None:
        return excluded
    rectangles = approval.findall("sourceContourExclusion")
    if rectangles and not approval.findall("protectedGap"):
        raise UsageError("A source contour exclusion requires a protected gap cross-section.")
    protected_gaps: dict[str, tuple[float, float, float, float]] = {}
    for gap in approval.findall("protectedGap"):
        gap_id = (gap.get("id") or "").strip()
        if not gap_id:
            continue
        if gap_id in protected_gaps:
            raise UsageError(f"Protected gap id '{gap_id}' is duplicated.")
        orientation = gap.get("orientation")
        try:
            fixed = int(gap.attrib["fixedCoordinate"])
            start = int(gap.attrib["start"])
            end = int(gap.attrib["end"])
        except (KeyError, ValueError) as error:
            raise UsageError("Protected gap coordinates are missing or invalid.") from error
        if orientation == "vertical":
            bounds = (float(fixed), float(start), float(fixed + 1), float(end + 1))
        elif orientation == "horizontal":
            bounds = (float(start), float(fixed), float(end + 1), float(fixed + 1))
        else:
            raise UsageError("Protected gap orientation must be horizontal or vertical.")
        protected_gaps[gap_id] = bounds
    for rectangle in rectangles:
        reason = (rectangle.get("reason") or "").strip()
        if not reason:
            raise UsageError("A source contour exclusion requires a semantic reason.")
        protected_gap_id = (rectangle.get("protectedGapId") or "").strip()
        if not protected_gap_id or protected_gap_id not in protected_gaps:
            raise UsageError(
                "A source contour exclusion must reference one existing protected gap id."
            )
        try:
            x = int(rectangle.attrib["x"])
            y = int(rectangle.attrib["y"])
            rectangle_width = int(rectangle.attrib["width"])
            rectangle_height = int(rectangle.attrib["height"])
        except (KeyError, ValueError) as error:
            raise UsageError("Source contour exclusion bounds are missing or invalid.") from error
        if (
            x < 0
            or y < 0
            or rectangle_width < 1
            or rectangle_height < 1
            or x + rectangle_width > width
            or y + rectangle_height > height
        ):
            raise UsageError("Source contour exclusion is outside the source canvas.")
        source_bounds_at_final = (
            x * final_width / width,
            y * final_height / height,
            (x + rectangle_width) * final_width / width,
            (y + rectangle_height) * final_height / height,
        )
        gap_bounds = protected_gaps[protected_gap_id]
        intersects_gap = (
            source_bounds_at_final[0] < gap_bounds[2]
            and source_bounds_at_final[2] > gap_bounds[0]
            and source_bounds_at_final[1] < gap_bounds[3]
            and source_bounds_at_final[3] > gap_bounds[1]
        )
        if not intersects_gap:
            raise UsageError(
                f"Source contour exclusion does not intersect protected gap "
                f"'{protected_gap_id}' at final scale."
            )
        local_bounds = (
            gap_bounds[0] - MAXIMUM_EXCLUSION_PADDING_FINAL_PIXELS,
            gap_bounds[1] - MAXIMUM_EXCLUSION_PADDING_FINAL_PIXELS,
            gap_bounds[2] + MAXIMUM_EXCLUSION_PADDING_FINAL_PIXELS,
            gap_bounds[3] + MAXIMUM_EXCLUSION_PADDING_FINAL_PIXELS,
        )
        is_local = (
            source_bounds_at_final[0] >= local_bounds[0]
            and source_bounds_at_final[1] >= local_bounds[1]
            and source_bounds_at_final[2] <= local_bounds[2]
            and source_bounds_at_final[3] <= local_bounds[3]
        )
        if not is_local:
            raise UsageError(
                f"Source contour exclusion for protected gap '{protected_gap_id}' "
                f"must stay within its {MAXIMUM_EXCLUSION_PADDING_FINAL_PIXELS:g}-pixel "
                "local neighborhood at final scale."
            )
        for current_y in range(y, y + rectangle_height):
            row = current_y * width
            for current_x in range(x, x + rectangle_width):
                excluded[row + current_x] = True
    return excluded


def validate_edge_clearance(
    image: Image.Image, final_width: int, final_height: int, minimum: float | None
) -> None:
    if minimum is None:
        return
    alpha = [pixel[3] for pixel in image.get_flattened_data()]
    visible = [
        (index % image.width, index // image.width)
        for index, value in enumerate(alpha)
        if value > 0
    ]
    if not visible:
        raise ApprovalError("Outlined result has no final-scale foreground.")
    horizontal_scale = image.width / final_width
    vertical_scale = image.height / final_height
    left = min(x for x, _ in visible) / horizontal_scale
    right = (image.width - 1 - max(x for x, _ in visible)) / horizontal_scale
    top = min(y for _, y in visible) / vertical_scale
    bottom = (image.height - 1 - max(y for _, y in visible)) / vertical_scale
    actual = min(left, right, top, bottom)
    if actual < minimum:
        raise ApprovalError(
            f"Post-outline normalized-source edge clearance is {actual:.6f} final pixels; "
            f"expected at least {minimum:.6f}."
        )


def validate_ring_thresholds(fractions: list[float], policy: ET.Element | None) -> None:
    if policy is None:
        return
    for index, fraction in enumerate(fractions, start=1):
        attribute = f"ring{index}MinimumDarkFraction"
        if policy.get(attribute) is None:
            continue
        try:
            minimum = float(policy.attrib[attribute])
        except ValueError as error:
            raise UsageError(f"{attribute} is invalid.") from error
        if minimum < 0.0 or minimum > 1.0:
            raise UsageError(f"{attribute} must be between zero and one.")
        if fraction + 1e-12 < minimum:
            raise ApprovalError(
                f"Post-outline ring {index} dark fraction is {fraction:.6f}; "
                f"expected at least {minimum:.6f}."
            )


def read_policy(
    baseline_path: Path | None,
    approval: ET.Element | None,
    source_width: int,
    source_height: int,
    final_width: int,
    final_height: int,
    stroke_pixels: int,
) -> tuple[ET.Element | None, int | None]:
    if approval is None:
        if baseline_path is not None:
            raise UsageError("Baseline requires an approved topology manifest and asset id.")
        return None, None
    if baseline_path is None or not baseline_path.is_file():
        raise UsageError("An existing outline baseline is required for approved processing.")
    class_id = approval.get("class")
    if not class_id:
        raise UsageError("Approved processing requires an asset class.")
    root = ET.parse(baseline_path).getroot()
    policies = [
        item
        for item in root.findall("./acceptanceClasses/class")
        if item.get("id") == class_id
    ]
    if len(policies) != 1:
        raise UsageError(
            f"Expected exactly one baseline acceptance class '{class_id}', found {len(policies)}."
        )
    policy = policies[0]
    permitted = policy.get("permittedAddedStrokeFinalPixels")
    if not permitted:
        raise UsageError(f"Acceptance class '{class_id}' has no permitted stroke range.")
    try:
        range_parts = permitted.split("-")
        if len(range_parts) == 1:
            minimum_final_pixels = maximum_final_pixels = float(range_parts[0])
        elif len(range_parts) == 2:
            minimum_final_pixels = float(range_parts[0])
            maximum_final_pixels = float(range_parts[1])
        else:
            raise ValueError("too many range separators")
        minimum_clearance = float(policy.attrib["minimumTransparentEdgeClearance"])
    except (KeyError, ValueError) as error:
        raise UsageError("Baseline stroke range or edge clearance is invalid.") from error
    if (
        minimum_final_pixels <= 0
        or maximum_final_pixels < minimum_final_pixels
        or minimum_clearance < 0
    ):
        raise UsageError("Baseline stroke range or edge clearance is invalid.")
    if final_width <= 0 or final_height <= 0:
        raise UsageError("Final approval dimensions must be positive.")
    minimum_scale = min(source_width / final_width, source_height / final_height)
    maximum_scale = max(source_width / final_width, source_height / final_height)
    if minimum_scale <= 0 or maximum_scale <= 0:
        raise UsageError("Source/final scale is invalid.")
    minimum_final_expansion = stroke_pixels / maximum_scale
    maximum_final_expansion = stroke_pixels / minimum_scale
    if minimum_final_expansion + 1e-12 < minimum_final_pixels:
        raise UsageError(
            f"Stroke expands as little as {minimum_final_expansion:.6f} final pixels; "
            f"class '{class_id}' requires at least {minimum_final_pixels}."
        )
    if maximum_final_expansion > maximum_final_pixels + 1e-12:
        raise UsageError(
            f"Stroke expands up to {maximum_final_expansion:.6f} final pixels; "
            f"class '{class_id}' permits at most {maximum_final_pixels}."
        )
    return policy, minimum_clearance


def dark_ring_fractions(image: Image.Image, count: int = 3) -> list[float]:
    width, height = image.size
    pixels = list(image.get_flattened_data())
    remaining = [pixel[3] >= FOREGROUND_ALPHA for pixel in pixels]
    fractions: list[float] = []
    for _ in range(count):
        alpha = [255 if selected else 0 for selected in remaining]
        exterior = exterior_background(alpha, width, height)
        ring: list[int] = []
        for index, selected in enumerate(remaining):
            if not selected:
                continue
            x = index % width
            y = index // width
            is_boundary = False
            for delta_y in (-1, 0, 1):
                next_y = y + delta_y
                if next_y < 0 or next_y >= height:
                    continue
                for delta_x in (-1, 0, 1):
                    if delta_x == 0 and delta_y == 0:
                        continue
                    next_x = x + delta_x
                    if next_x < 0 or next_x >= width:
                        continue
                    if exterior[next_y * width + next_x]:
                        is_boundary = True
                        break
                if is_boundary:
                    break
            if is_boundary:
                ring.append(index)
        if not ring:
            fractions.append(0.0)
            continue
        dark = 0
        for index in ring:
            red, green, blue, _ = pixels[index]
            if 0.2126 * red + 0.7152 * green + 0.0722 * blue <= DARK_LUMA:
                dark += 1
            remaining[index] = False
        fractions.append(dark / len(ring))
    return fractions


def save_png(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=False, compress_level=9)


def publish_outputs(
    diffuse: Image.Image,
    diffuse_path: Path,
    mask: Image.Image | None,
    mask_path: Path | None,
) -> tuple[str, str | None]:
    diffuse_path.parent.mkdir(parents=True, exist_ok=True)
    if mask_path is not None:
        mask_path.parent.mkdir(parents=True, exist_ok=True)
    suffix = uuid.uuid4().hex
    diffuse_temp = diffuse_path.with_name(f".{diffuse_path.name}.{suffix}.tmp")
    mask_temp = (
        mask_path.with_name(f".{mask_path.name}.{suffix}.tmp")
        if mask_path is not None
        else None
    )
    published: list[Path] = []
    try:
        save_png(diffuse, diffuse_temp)
        diffuse_hash = sha256(diffuse_temp)
        mask_hash: str | None = None
        if mask is not None and mask_temp is not None:
            save_png(mask, mask_temp)
            mask_hash = sha256(mask_temp)
        os.link(diffuse_temp, diffuse_path)
        published.append(diffuse_path)
        if mask_temp is not None and mask_path is not None:
            os.link(mask_temp, mask_path)
            published.append(mask_path)
        return diffuse_hash, mask_hash
    except Exception:
        for path in reversed(published):
            try:
                path.unlink()
            except FileNotFoundError:
                pass
        raise
    finally:
        for path in (diffuse_temp, mask_temp):
            if path is not None:
                try:
                    path.unlink()
                except FileNotFoundError:
                    pass


def process(args: argparse.Namespace) -> dict[str, object]:
    require_exact_runtime()
    if args.baseline is None or args.topology_manifest is None or args.asset_id is None:
        raise UsageError("Baseline, topology manifest, and asset id are required.")
    if not args.input.is_file() or args.input.suffix.lower() != ".png":
        raise UsageError(f"Input must be an existing PNG: {args.input}")
    if args.output.suffix.lower() != ".png":
        raise UsageError("Output must be a PNG path.")
    if args.output.exists():
        raise UsageError(f"Output already exists: {args.output}")
    if (args.mask_input is None) != (args.mask_output is None):
        raise UsageError("Mask input and output must be supplied together.")
    if args.mask_input is not None and (
        not args.mask_input.is_file()
        or args.mask_input.suffix.lower() != ".png"
        or args.mask_output.suffix.lower() != ".png"
    ):
        raise UsageError("Mask input/output must be PNG paths and the input must exist.")
    if args.mask_input is not None and args.mask_input.resolve() == args.mask_output.resolve():
        raise UsageError("Mask output must differ from mask input.")
    if args.mask_output is not None and args.mask_output.exists():
        raise UsageError(f"Mask output already exists: {args.mask_output}")
    image_paths = [args.input.resolve(), args.output.resolve()]
    if args.mask_input is not None and args.mask_output is not None:
        image_paths.extend((args.mask_input.resolve(), args.mask_output.resolve()))
    if len(set(image_paths)) != len(image_paths):
        raise UsageError("Diffuse and mask input/output paths must all be distinct.")

    outline_color = parse_color(args.outline_color)
    approval, final_width, final_height = read_approval(
        args.topology_manifest, args.asset_id, args.input
    )
    approved_outline_color = approval.get("outlineColor") if approval is not None else None
    if (
        approved_outline_color is not None
        and outline_color != parse_color(approved_outline_color)
    ):
        raise UsageError(
            f"Outline color {args.outline_color.upper()} does not match approval "
            f"{approved_outline_color.upper()} for '{args.asset_id}'."
        )
    with Image.open(args.input) as opened:
        source = opened.convert("RGBA")
    source_pixels = list(source.get_flattened_data())
    width, height = source.size
    policy, minimum_clearance = read_policy(
        args.baseline,
        approval,
        width,
        height,
        final_width,
        final_height,
        args.stroke_pixels,
    )
    source_alpha = [pixel[3] for pixel in source_pixels]
    excluded = source_contour_exclusions(
        approval, width, height, final_width, final_height
    )
    source_final = resize_for_approval(source, final_width, final_height)
    validate_approval(source_final, approval, "Pre-outline")

    exterior = exterior_background(source_alpha, width, height)
    contour_alpha = antialiased_contour_alpha(
        source_alpha, width, height, args.stroke_pixels
    )
    outlined_pixels = list(source_pixels)
    added_indices: list[int] = []
    for index, alpha in enumerate(source_alpha):
        if (
            alpha == 0
            and exterior[index]
            and not excluded[index]
            and contour_alpha[index] > 0
        ):
            outlined_pixels[index] = (*outline_color[:3], contour_alpha[index])
            added_indices.append(index)
    outlined = Image.new("RGBA", source.size)
    outlined.putdata(outlined_pixels)

    outlined_final = resize_for_approval(outlined, final_width, final_height)
    validate_approval(outlined_final, approval, "Post-outline")
    validate_edge_clearance(outlined, final_width, final_height, minimum_clearance)
    fractions = dark_ring_fractions(outlined_final)
    validate_ring_thresholds(fractions, policy)

    mask_output: Image.Image | None = None
    if args.mask_input is not None:
        with Image.open(args.mask_input) as opened:
            source_mask = opened.convert("RGBA")
        if source_mask.size != source.size:
            raise UsageError("Mask and diffuse dimensions differ.")
        mask_pixels = list(source_mask.get_flattened_data())
        for index, (mask_pixel, diffuse_pixel) in enumerate(zip(mask_pixels, source_pixels)):
            if mask_pixel[3] != diffuse_pixel[3]:
                raise UsageError(f"Mask and diffuse alpha differ at source pixel {index}.")
        for index in added_indices:
            mask_pixels[index] = (0, 0, 0, contour_alpha[index])
        mask_output = Image.new("RGBA", source.size)
        mask_output.putdata(mask_pixels)
        if [pixel[3] for pixel in mask_output.get_flattened_data()] != [
            pixel[3] for pixel in outlined.get_flattened_data()
        ]:
            raise ApprovalError("Outlined mask alpha does not equal outlined diffuse alpha.")

    output_hash, mask_output_hash = publish_outputs(
        outlined, args.output, mask_output, args.mask_output
    )

    components, holes = topology(outlined_final)
    result: dict[str, object] = {
        "input": str(args.input.resolve()),
        "output": str(args.output.resolve()),
        "strokePixels": args.stroke_pixels,
        "outlineColor": args.outline_color.upper(),
        "addedPixels": len(added_indices),
        "sourceSha256": sha256(args.input),
        "outputSha256": output_hash,
        "finalWidth": final_width,
        "finalHeight": final_height,
        "foregroundComponents": components,
        "backgroundHoles": holes,
        "ring1": round(fractions[0], 6),
        "ring2": round(fractions[1], 6),
        "ring3": round(fractions[2], 6),
        "maskOutput": str(args.mask_output.resolve()) if args.mask_output is not None else None,
        "maskOutputSha256": mask_output_hash,
    }
    return result


def main() -> int:
    args = parse_arguments()
    try:
        result = process(args)
    except UsageError as error:
        print(str(error), file=sys.stderr)
        return 2
    except ApprovalError as error:
        print(str(error), file=sys.stderr)
        return 1
    except Exception as error:  # keep one failed asset isolated and actionable
        print(f"Could not add RimWorld sprite outline: {error}", file=sys.stderr)
        return 1

    if args.output_mode == "json":
        print(json.dumps(result, sort_keys=True, separators=(",", ":")))
    else:
        for key, value in result.items():
            print(f"{key}: {value}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
