"""Blender 5.2: encode completed Unity bathroom captures at their original 24 fps.

Run with: blender --background --python ArtSource/Blender/encode_bathroom_motion.py
Optional arguments after --: activity names, including the three Detail names.
Without arguments, the optional details are included only when all three capture
manifests report successful completion; the five original recordings stay intact.
Only numbered JPG frames in Library/BathroomMotion are used; no retiming occurs.
"""
from pathlib import Path
import hashlib
import json
import struct
import sys

import bpy


ROOT = Path(__file__).resolve().parents[2]
KINDS = ("LitterDig", "MatKnead", "TubEdgeWalk", "ShowerRinse", "MirrorGaze")
DETAIL_KINDS = ("LitterDigDetail", "MatKneadDetail", "ShowerRinseDetail")
FPS = 24
DESTINATION = ROOT / "Docs/QA/BATHROOM_POLISH_2026-09-09/videos"


def jpeg_dimensions(path):
    """Read SOF dimensions without requiring Pillow inside Blender."""
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
            if marker[0] == 0x00 or marker[0] in range(0xD0, 0xD9):
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
    folder = ROOT / "Library/BathroomMotion" / kind
    frames = sorted(path for path in folder.glob("*.jpg")
                    if len(path.stem) == 4 and path.stem.isascii() and path.stem.isdigit())
    if not frames:
        raise ValueError(f"No captured frames for {kind}: {folder}")
    if [path.name for path in frames] != [f"{index:04d}.jpg" for index in range(len(frames))]:
        raise ValueError(f"{kind}: frames must be contiguous, beginning at 0000.jpg")
    if not 240 <= len(frames) <= 432:
        raise ValueError(f"{kind}: expected a complete 10–18 second routine; found {len(frames)} frames")
    manifest_path = folder / "capture.json"
    if not manifest_path.exists():
        raise ValueError(f"{kind}: a successful capture.json is required before encoding")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if (manifest.get("kind") != kind or manifest.get("status") != "Complete"
            or manifest.get("error") or manifest.get("fps") != FPS
            or manifest.get("frames") != len(frames) or manifest.get("completionCount") != 1
            or abs(manifest.get("seconds", 0) - len(frames) / FPS) > .002):
        raise ValueError(f"{kind}: capture manifest is incomplete or disagrees with its frames")
    if kind in DETAIL_KINDS and (manifest.get("detail") is not True or manifest.get("activityKind") != kind.removesuffix("Detail")):
        raise ValueError(f"{kind}: detail manifest does not identify its real activity")
    for frame in frames:
        if jpeg_dimensions(frame) != (1920, 1080):
            raise ValueError(f"{kind}: frame is not 1920×1080: {frame.name}")
    return frames, manifest


def encode(kind, frames, capture_manifest):
    scene = bpy.context.scene
    scene.sequence_editor_clear()
    editor = scene.sequence_editor_create()
    strip = editor.strips.new_image(kind, str(frames[0]), channel=1, frame_start=1)
    for frame in frames[1:]:
        strip.elements.append(frame.name)
    strip.frame_final_duration = len(frames)
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    scene.render.fps = FPS
    scene.render.fps_base = 1.0
    scene.frame_start = 1
    scene.frame_end = len(frames)
    scene.render.frame_map_old = 100
    scene.render.frame_map_new = 100
    scene.render.use_sequencer = True
    scene.render.use_compositing = False
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1
    scene.render.image_settings.media_type = "VIDEO"
    scene.render.ffmpeg.format = "MPEG4"
    scene.render.ffmpeg.codec = "H264"
    scene.render.ffmpeg.constant_rate_factor = "HIGH"
    scene.render.ffmpeg.ffmpeg_preset = "GOOD"
    scene.render.ffmpeg.audio_codec = "NONE"
    destination = DESTINATION / f"{kind}.mp4"
    # A failed encode cannot leave an older verification sidecar for a partial video.
    sidecar = destination.with_suffix(".json")
    sidecar.unlink(missing_ok=True)
    scene.render.filepath = str(destination)
    bpy.ops.render.render(animation=True)
    if not destination.exists() or destination.stat().st_size < 1024:
        raise RuntimeError(f"Blender did not produce a usable MP4: {destination}")
    metadata = {
        "kind": kind, "status": "Complete", "fps": FPS, "frames": len(frames),
        "seconds": len(frames) / FPS, "width": 1920, "height": 1080,
        "codec": "H264", "viewTransform": "Standard", "retimed": False,
        "captureManifestVerified": True, "completionCount": capture_manifest["completionCount"],
        "detail": kind in DETAIL_KINDS, "activityKind": kind.removesuffix("Detail"),
        "bytes": destination.stat().st_size,
        "sha256": hashlib.sha256(destination.read_bytes()).hexdigest(),
        "source": str(frames[0].parent.relative_to(ROOT)).replace("\\", "/"),
    }
    sidecar.write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(metadata, ensure_ascii=False), flush=True)


def main():
    requested = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(KINDS)
    if "--" not in sys.argv:
        complete_details = []
        for kind in DETAIL_KINDS:
            manifest_path = ROOT / "Library/BathroomMotion" / kind / "capture.json"
            try:
                manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
                complete_details.append(manifest.get("status") == "Complete" and manifest.get("completionCount") == 1)
            except (OSError, ValueError):
                complete_details.append(False)
        if all(complete_details):
            requested.extend(DETAIL_KINDS)
        else:
            print("Optional detail set is not complete; encoding the five wide recordings only.", flush=True)
    supported = KINDS + DETAIL_KINDS
    if not requested or len(set(requested)) != len(requested) or any(kind not in supported for kind in requested):
        raise ValueError("Specify distinct supported activity names: " + ", ".join(supported))
    if bpy.app.version < (5, 2, 0):
        raise RuntimeError("This encoder uses the Blender 5.2 video sequence API.")
    # Validate the entire requested set before replacing any existing output.
    captures = [(kind, *capture_frames(kind)) for kind in requested]
    DESTINATION.mkdir(parents=True, exist_ok=True)
    for kind, frames, manifest in captures:
        encode(kind, frames, manifest)


if __name__ == "__main__":
    main()
