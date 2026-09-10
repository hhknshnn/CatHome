"""Encode actual Unity frames at their captured 24 fps, with no retiming."""
from pathlib import Path
import bpy

root=Path(__file__).resolve().parents[2]
destination=root/'Docs/QA/CARE_MOTION_2026-09-09/motion'
destination.mkdir(parents=True,exist_ok=True)
for folder in sorted((root/'Library/ComfortMotion').iterdir()):
    frames=sorted(folder.glob('*.jpg'))
    if not frames:
        continue
    scene=bpy.context.scene
    scene.sequence_editor_clear()
    editor=scene.sequence_editor_create()
    strip=editor.strips.new_image(folder.name,str(frames[0]),channel=1,frame_start=1)
    for frame in frames[1:]:
        strip.elements.append(frame.name)
    strip.frame_final_duration=len(frames)
    scene.render.resolution_x=1920
    scene.render.resolution_y=1080
    scene.render.resolution_percentage=100
    scene.render.fps=24
    scene.frame_start=1
    scene.frame_end=len(frames)
    scene.view_settings.view_transform='Standard'
    scene.view_settings.look='None'
    scene.render.image_settings.media_type='VIDEO'
    scene.render.ffmpeg.format='MPEG4'
    scene.render.ffmpeg.codec='H264'
    scene.render.ffmpeg.constant_rate_factor='HIGH'
    scene.render.ffmpeg.ffmpeg_preset='GOOD'
    scene.render.filepath=str(destination/(folder.name+'.mp4'))
    bpy.ops.render.render(animation=True)
