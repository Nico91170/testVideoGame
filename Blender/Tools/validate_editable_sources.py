"""Reopen every editable Blender source and test a disposable mesh edit/export.
Does not overwrite sources or game FBX. Run via blender -b --python <thisfile>.
"""
import bpy, json, tempfile
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
source=json.loads((ROOT/'Blender/asset_report.json').read_text())
report=[]
for record in source:
    name=record['model']
    paths=list((ROOT/'Blender').glob('*/'+name+'.blend'))
    assert len(paths)==1,(name,'unique editable source')
    bpy.ops.wm.open_mainfile(filepath=str(paths[0]))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(meshes)==record['meshes'],(name,'source pieces')
    assert sum(len(o.data.vertices) for o in meshes)==record['vertices'],(name,'editable vertex count')
    assert all(len(o.data.materials)>0 for o in meshes),(name,'material assignment')
    report.append({'model':name,'editable_meshes':len(meshes),'result':'PASS'})

# Confirm a real source edit survives an export/reimport. Only a temporary FBX is written.
nico=ROOT/'Blender/Characters/Nico.blend'
bpy.ops.wm.open_mainfile(filepath=str(nico))
bpy.context.scene.frame_set(1)
obj=bpy.data.objects['Track_Zipper']; obj.data.vertices[0].co.x+=.093
expected=obj.matrix_world@obj.data.vertices[0].co
with tempfile.TemporaryDirectory(prefix='rive404-mesh-roundtrip-') as folder:
    path=Path(folder)/'EditedNico.fbx'
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,axis_forward='-Z',axis_up='Y',
        object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False)
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False)
    edited=bpy.data.objects['Track_Zipper']
    points=[edited.matrix_world@v.co for v in edited.data.vertices]
    error=min((p-expected).length for p in points)
    assert error<.0001,('edited vertex preserved',error)

# The unsaved temporary edit was never written to Nico.blend or Resources/Models/Nico.fbx.
result={'tool':'Blender '+bpy.app.version_string,'sources':report,
        'disposable_edit_export_reimport':{'model':'Nico','edit_m':.093,'position_error_m':error,'result':'PASS'},
        'scope':'Source editing and FBX round-trip in Blender; Unity reimport untested'}
(ROOT/'Blender/editable_sources_report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('RIVE404_EDITABLE_SOURCES '+json.dumps(result))
