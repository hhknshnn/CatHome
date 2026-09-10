"""Encode verified, complete Unity captures without changing their timing.

Blender 5.2 headless:
  blender --background --factory-startup --python <this file>
  blender --background --factory-startup --python <this file> -- PaperSpin

No arguments require all eight captures. An explicit label encodes only that
capture. A success sidecar is written only after the MP4 sample table and a
fresh Blender MovieStrip agree with every source frame and its native 24 fps.
"""
from pathlib import Path
import hashlib
import json
import math
import struct
import sys

import bpy


ROOT = Path(__file__).resolve().parents[3]
KINDS = ("PaperSpin", "LitterDig", "MatKnead", "TubEdgeWalk", "ShowerRinse",
         "MirrorGaze", "PaperSpinDetail", "LitterDigDetail")
FPS = 24
WIDTH, HEIGHT = 1920, 1080
DESTINATION = ROOT / "Docs/QA/CAMERA_FACING_2026-09-09/videos"


def jpeg_dimensions(path):
    """Read JPEG SOF dimensions; Blender's Python needs no extra packages."""
    with path.open("rb") as stream:
        if stream.read(2) != b"\xff\xd8":
            raise ValueError(f"Not a JPEG: {path}")
        while True:
            prefix = stream.read(1)
            if not prefix:
                break
            if prefix != b"\xff":
                continue
            marker = stream.read(1)
            while marker == b"\xff":
                marker = stream.read(1)
            if not marker or marker[0] in (0xD9, 0xDA):
                break
            if marker[0] == 0 or 0xD0 <= marker[0] <= 0xD8:
                continue
            length_bytes = stream.read(2)
            if len(length_bytes) != 2:
                break
            length = struct.unpack(">H", length_bytes)[0]
            if length < 2:
                break
            if marker[0] in (0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7,
                             0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF):
                payload = stream.read(5)
                if len(payload) == 5:
                    height, width = struct.unpack(">HH", payload[1:])
                    return width, height
                break
            stream.seek(length - 2, 1)
    raise ValueError(f"JPEG dimensions unavailable: {path}")


def capture_frames(kind):
    folder = ROOT / "Library/CameraFacingMotion" / kind
    manifest_path = folder / "capture.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    frames = sorted(folder.glob("*.jpg"))
    if not frames or [p.name for p in frames] != [f"{i:04d}.jpg" for i in range(len(frames))]:
        raise ValueError(f"{kind}: require only contiguous JPG frames starting at 0000.jpg")
    seconds = manifest.get("seconds")
    if (manifest.get("kind") != kind or manifest.get("status") != "Complete"
            or manifest.get("error") != "" or manifest.get("fps") != FPS
            or manifest.get("frames") != len(frames) or manifest.get("completionCount") != 1
            or not isinstance(seconds, (int, float)) or not math.isfinite(seconds)
            or abs(seconds - len(frames) / FPS) > .002
            or manifest.get("detail") is not kind.endswith("Detail")
            or manifest.get("activityKind") != kind.removesuffix("Detail")):
        raise ValueError(f"{kind}: capture manifest is incomplete or disagrees with its frames")
    for frame in frames:
        if jpeg_dimensions(frame) != (WIDTH, HEIGHT):
            raise ValueError(f"{kind}: frame is not {WIDTH}x{HEIGHT}: {frame.name}")
    return frames, manifest, hashlib.sha256(manifest_path.read_bytes()).hexdigest()


def boxes(data, begin=0, end=None):
    """Read bounded ISO BMFF boxes, including extended sizes."""
    end = len(data) if end is None else end
    offset = begin
    while offset < end:
        if offset + 8 > end:
            raise ValueError("Truncated MP4 box header")
        size, kind = struct.unpack_from(">I4s", data, offset)
        header = 8
        if size == 1:
            if offset + 16 > end:
                raise ValueError("Truncated extended MP4 box")
            size = struct.unpack_from(">Q", data, offset + 8)[0]
            header = 16
        elif size == 0:
            size = end - offset
        if size < header or offset + size > end:
            raise ValueError(f"Invalid MP4 box size: {kind!r}")
        yield kind, offset + header, offset + size
        offset += size


def child(data, parent, name):
    matches = [(start, end) for kind, start, end in boxes(data, *parent) if kind == name]
    if len(matches) != 1:
        raise ValueError(f"Expected exactly one MP4 {name!r} box")
    return matches[0]


def verify_container(path, count):
    data = path.read_bytes()
    top = list(boxes(data))
    if len(data) < 1024 or not top or top[0][0] != b"ftyp":
        raise ValueError(f"Not a usable MP4: {path}")
    if top[0][2] - top[0][1] < 8 or not any(k == b"mdat" and b > a for k, a, b in top):
        raise ValueError("MP4 has no media payload")
    moov = child(data, (0, len(data)), b"moov")
    video_samples = []
    for kind, start, end in boxes(data, *moov):
        if kind != b"trak":
            continue
        mdia = child(data, (start, end), b"mdia")
        hdlr = child(data, mdia, b"hdlr")
        if hdlr[1] - hdlr[0] < 12:
            raise ValueError("Truncated MP4 track handler")
        if data[hdlr[0] + 8:hdlr[0] + 12] != b"vide":
            continue
        stbl = child(data, child(data, mdia, b"minf"), b"stbl")
        stsd = child(data, stbl, b"stsd")
        if stsd[1] - stsd[0] < 8:
            raise ValueError("Truncated MP4 sample descriptions")
        entries = list(boxes(data, stsd[0] + 8, stsd[1]))
        if len(entries) != 1 or entries[0][0] not in (b"avc1", b"avc3"):
            raise ValueError("Expected a single H264/AVC video sample description")
        stsz = child(data, stbl, b"stsz")
        if stsz[1] - stsz[0] < 12:
            raise ValueError("Truncated MP4 sample-size table")
        sample_size, sample_count = struct.unpack_from(">II", data, stsz[0] + 4)
        if not sample_size and stsz[1] - stsz[0] != 12 + 4 * sample_count:
            raise ValueError("Truncated MP4 video sample-size entries")
        video_samples.append(sample_count)
    if video_samples != [count]:
        raise ValueError(f"MP4 video sample counts {video_samples} do not match {count} source frames")
    return {"containerHeaderVerified": True, "containerVideoSamples": count}


def verify_movie(path, count):
    """Reopen the encoded media. This is a decoder/import probe, not a pixel comparison."""
    scene = bpy.data.scenes.new("CameraFacingMovieVerification")
    try:
        scene.render.fps = FPS
        scene.render.fps_base = 1.0
        editor = scene.sequence_editor_create()
        movie = editor.strips.new_movie("EncodedCapture", str(path), channel=1, frame_start=1)
        if len(movie.elements) != 1:
            raise ValueError("Blender did not load the encoded movie stream")
        element = movie.elements[0]
        decoded_frames = int(movie.frame_duration)
        width, height, fps = element.orig_width, element.orig_height, float(movie.fps)
        if (decoded_frames != count or movie.frame_final_duration != count
                or (width, height) != (WIDTH, HEIGHT) or abs(fps - FPS) > .0001):
            raise ValueError(f"Reopened movie disagrees: {decoded_frames} frames, {width}x{height}, {fps} fps")
        return {"decodedFrames": decoded_frames, "decodedWidth": width, "decodedHeight": height,
                "decodedFps": fps, "decoder": "Blender MovieStrip", "videoDecodeVerified": True,
                "decodeVerification": "MovieStrip source duration/dimensions/fps and AVC sample count; no pixel comparison"}
    finally:
        scene.sequence_editor_clear()
        bpy.data.scenes.remove(scene)


def encode(kind, frames, manifest, manifest_hash):
    scene = bpy.context.scene
    scene.sequence_editor_clear()
    editor = scene.sequence_editor_create()
    strip = editor.strips.new_image(kind, str(frames[0]), channel=1, frame_start=1)
    for frame in frames[1:]:
        strip.elements.append(frame.name)
    strip.frame_final_duration = len(frames)
    strip.colorspace_settings.name = "sRGB"
    scene.render.resolution_x, scene.render.resolution_y = WIDTH, HEIGHT
    scene.render.resolution_percentage = 100
    scene.render.pixel_aspect_x = scene.render.pixel_aspect_y = 1.0
    scene.render.fps, scene.render.fps_base = FPS, 1.0
    scene.frame_start, scene.frame_end, scene.frame_step = 1, len(frames), 1
    scene.render.frame_map_old = scene.render.frame_map_new = 100
    scene.render.use_sequencer, scene.render.use_compositing = True, False
    scene.view_settings.view_transform, scene.view_settings.look = "Standard", "None"
    scene.view_settings.exposure, scene.view_settings.gamma = 0.0, 1.0
    scene.render.image_settings.media_type = "VIDEO"
    scene.render.ffmpeg.format, scene.render.ffmpeg.codec = "MPEG4", "H264"
    scene.render.ffmpeg.constant_rate_factor, scene.render.ffmpeg.ffmpeg_preset = "HIGH", "GOOD"
    scene.render.ffmpeg.audio_codec = "NONE"
    destination = DESTINATION / f"{kind}.mp4"
    temporary = DESTINATION / f"{kind}.encoding.mp4"
    sidecar = destination.with_suffix(".json")
    # Failed rendering/verification cannot retain a previous success certificate.
    sidecar.unlink(missing_ok=True)
    temporary.unlink(missing_ok=True)
    scene.render.filepath = str(temporary)
    bpy.ops.render.render(animation=True)
    container = verify_container(temporary, len(frames))
    decoded = verify_movie(temporary, len(frames))
    # The capture must still describe the same completed recording after rendering.
    current_manifest = frames[0].parent / "capture.json"
    if hashlib.sha256(current_manifest.read_bytes()).hexdigest() != manifest_hash:
        raise ValueError(f"{kind}: capture manifest changed while encoding")
    temporary.replace(destination)
    metadata = {
        "kind": kind, "status": "Complete", "fps": FPS, "frames": len(frames),
        "seconds": len(frames) / FPS, "width": WIDTH, "height": HEIGHT,
        "codec": "H264", "viewTransform": "Standard", "look": "None", "retimed": False,
        "captureManifestVerified": True, "captureManifestSha256": manifest_hash,
        "completionCount": manifest["completionCount"], "detail": kind.endswith("Detail"),
        "activityKind": kind.removesuffix("Detail"), "bytes": destination.stat().st_size,
        "sha256": hashlib.sha256(destination.read_bytes()).hexdigest(),
        "source": frames[0].parent.relative_to(ROOT).as_posix(), **container, **decoded,
    }
    sidecar_temporary = sidecar.with_suffix(".json.tmp")
    sidecar_temporary.write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    sidecar_temporary.replace(sidecar)
    print(json.dumps(metadata, ensure_ascii=False), flush=True)


def main():
    explicit = "--" in sys.argv
    requested = sys.argv[sys.argv.index("--") + 1:] if explicit else list(KINDS)
    if not requested or (explicit and len(requested) != 1) or any(kind not in KINDS for kind in requested):
        raise ValueError("After -- specify one supported label: " + ", ".join(KINDS))
    if bpy.app.version < (5, 2, 0):
        raise RuntimeError("Blender 5.2 or later is required for this sequence API")
    captures = [(kind, *capture_frames(kind)) for kind in requested]
    DESTINATION.mkdir(parents=True, exist_ok=True)
    for capture in captures:
        encode(*capture)


if __name__ == "__main__":
    main()
