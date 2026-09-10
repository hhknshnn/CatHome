"""Encode the game's own full-HD capture; headless Blender's bundled video encoder."""
from pathlib import Path
import bpy
root=Path(__file__).resolve().parents[3]
frames=sorted((root/'Library/CatTelevisionFrames').glob('*.png'))
if len(frames)!=240:raise RuntimeError('Expected exactly 240 native Unity frames')
scene=bpy.context.scene
scene.render.resolution_x=1920;scene.render.resolution_y=1080;scene.render.resolution_percentage=100
scene.render.fps=24;scene.frame_start=1;scene.frame_end=len(frames)
editor=scene.sequence_editor_create()
strip=editor.strips.new_image('Our cats',str(frames[0]),channel=1,frame_start=1)
for frame in frames[1:]:strip.elements.append(frame.name)
strip.frame_final_duration=len(frames)
scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
scene.render.image_settings.media_type='VIDEO'
scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
scene.render.ffmpeg.constant_rate_factor='HIGH';scene.render.ffmpeg.ffmpeg_preset='GOOD'
destination=root/'Assets/Art/Television';destination.mkdir(parents=True,exist_ok=True)
scene.render.filepath=str(destination/'CatHomeTelevision.mp4')
bpy.ops.render.render(animation=True)
