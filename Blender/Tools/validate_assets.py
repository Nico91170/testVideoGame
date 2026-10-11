"""FBX round-trip validation, WAV format checks and optional contact sheet rendering.
blender -b --python Blender/Tools/validate_assets.py [-- --render]
Validation is independent from Unity; it does not prove Unity imports/builds.
"""
import bpy, json, sys, wave, math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
MODELS=ROOT/'UnityProject/Assets/_RIVE404/Resources/Models'
REPORT=[]
source=json.loads((ROOT/'Blender/asset_report.json').read_text())
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

for record in source:
    name=record['model']; path=MODELS/(name+'.fbx')
    assert path.read_bytes().startswith(b'Kaydara FBX Binary '),name+' FBX header'
    bpy.ops.import_scene.fbx(filepath=str(path),use_anim=True)
    imported=list(bpy.context.selected_objects)
    meshes=[o for o in imported if o.type=='MESH']; rigs=[o for o in imported if o.type=='ARMATURE']
    assert len(meshes)==1,(name,'one merged renderer expected',len(meshes))
    assert sum(len(o.data.vertices) for o in meshes)==record['vertices'],(name,'vertex count')
    assert sum(len(o.data.bones) for o in rigs)==record['bones'],(name,'bone count')
    points=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
    low=Vector([min(v[a] for v in points) for a in range(3)])
    high=Vector([max(v[a] for v in points) for a in range(3)])
    dimensions=high-low
    assert low.z>=-.10,(name,'floor pivot',list(low))
    if name in ('Nico','Sillage','NPC','Corrupted'):
        assert 1.75<dimensions.z<2.10,(name,'character metric height',list(dimensions))
        # Root drives the rig transform and intentionally owns no mesh weights.
        assert len(meshes[0].vertex_groups)>=13,(name,'weights',len(meshes[0].vertex_groups))
        assert len([a for a in bpy.data.actions if name in a.name])>=2,(name,'actions')
    REPORT.append({'model':name,'import_meshes':len(meshes),'vertices':len(meshes[0].data.vertices),
        'bones':sum(len(o.data.bones) for o in rigs),'bounds_min':[round(v,4) for v in low],
        'dimensions_m':[round(v,4) for v in dimensions],'result':'PASS'})
    for obj in imported: bpy.data.objects.remove(obj,do_unlink=True)

audio=[]
for path in sorted((ROOT/'UnityProject/Assets/_RIVE404/Resources/Audio').glob('*.wav')):
    with wave.open(str(path),'rb') as w:
        assert w.getnchannels()==1 and w.getsampwidth()==2 and w.getframerate()==22050
        assert w.getnframes()>0
        audio.append({'name':path.stem,'seconds':round(w.getnframes()/w.getframerate(),2),'result':'PASS'})

result={'tool':'Blender '+bpy.app.version_string,'scope':'FBX reimport in Blender and PCM WAV; Unity import untested',
        'models':REPORT,'audio':audio}
(ROOT/'Blender/validation_report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('RIVE404_VALIDATION '+json.dumps(result))

if '--render' in sys.argv:
    # A readable studio contact sheet, real geometry rendered by Blender CPU Cycles.
    for index,record in enumerate(source):
        name=record['model']; bpy.ops.import_scene.fbx(filepath=str(MODELS/(name+'.fbx')),use_anim=False)
        imported=list(bpy.context.selected_objects)
        meshes=[o for o in imported if o.type=='MESH']
        pts=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
        low=Vector([min(v[a] for v in pts) for a in range(3)]); hi=Vector([max(v[a] for v in pts) for a in range(3)])
        # Apply render fit on a parent empty so rigs and nested transforms stay intact.
        parent=bpy.data.objects.new('Preview_'+name,None); bpy.context.collection.objects.link(parent)
        for obj in imported:
            if obj.parent not in imported:
                matrix=obj.matrix_world.copy(); obj.parent=parent; obj.matrix_world=matrix
        k=min(2.5/max(hi.x-low.x,hi.y-low.y),3/(hi.z-low.z))
        x=(index%4-1.5)*4; y=(index//4-1.5)*4
        parent.scale=(k,k,k); parent.location=(x-(low.x+hi.x)*k/2,y-(low.y+hi.y)*k/2,-low.z*k)
        bpy.ops.object.text_add(location=(x-1.45,y-1.75,.015),rotation=(0,0,0))
        txt=bpy.context.object; txt.name='Label_'+name; txt.data.body=name; txt.data.size=.35
        txt.data.extrude=.001; txt.data.align_x='LEFT'
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.03))
    floor=bpy.context.object; mat=bpy.data.materials.new('PreviewFloor'); mat.diffuse_color=(.18,.22,.25,1)
    mat.use_nodes=True; mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.18,.22,.25,1)
    mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.9; floor.data.materials.append(mat)
    scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=24
    scene.cycles.use_denoising=False
    scene.world.use_nodes=True; scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.45,.53,.65,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
    bpy.ops.object.light_add(type='AREA',location=(-8,-10,18)); light=bpy.context.object; light.data.energy=3200; light.data.shape='DISK'; light.data.size=12
    light.rotation_euler=(Vector((0,0,0))-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=(9,-20,25)); camera=bpy.context.object; camera.data.type='ORTHO'; camera.data.ortho_scale=22
    camera.rotation_euler=(Vector((0,0,.5))-camera.location).to_track_quat('-Z','Y').to_euler(); scene.camera=camera
    scene.render.resolution_x=1400; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
    scene.view_settings.view_transform='AgX'; scene.render.image_settings.file_format='PNG'
    dest=ROOT/'Blender/Preview/Kit_Overview.png'; dest.parent.mkdir(parents=True,exist_ok=True)
    scene.render.filepath=str(dest); bpy.ops.render.render(write_still=True)
