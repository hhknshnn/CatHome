"""Encode actual Unity frames at their recorded 24fps; never retime the animation."""
from pathlib import Path
import bpy
root=Path(__file__).resolve().parents[3]
out=root/'Docs/QA/ROOM_POLISH_2026-09-07/motion';out.mkdir(exist_ok=True)
for folder in sorted((root/'Library/RoomPolishMotionFrames').iterdir()):
    final=root/'Library/RoomPolishMotionFramesFinal'/folder.name
    if final.is_dir():folder=final
    frames=sorted(folder.glob('*.jpg'))
    if not frames:continue
    s=bpy.context.scene;s.sequence_editor_clear();editor=s.sequence_editor_create()
    strip=editor.strips.new_image(folder.name,str(frames[0]),channel=1,frame_start=1)
    for frame in frames[1:]:strip.elements.append(frame.name)
    strip.frame_final_duration=len(frames)
    s.render.resolution_x=1920;s.render.resolution_y=1080;s.render.resolution_percentage=100
    s.render.fps=24;s.frame_start=1;s.frame_end=len(frames)
    s.view_settings.view_transform='Standard';s.view_settings.look='None'
    s.render.image_settings.media_type='VIDEO';s.render.ffmpeg.format='MPEG4';s.render.ffmpeg.codec='H264'
    s.render.ffmpeg.constant_rate_factor='HIGH';s.render.ffmpeg.ffmpeg_preset='GOOD'
    s.render.filepath=str(out/(folder.name+'.mp4'));bpy.ops.render.render(animation=True)
    print(folder.name,len(frames),'frames at 24 fps')
