"""Export the shared cat rig and its three actions without saving the source blend."""
import bpy
import json
import os
import sys
from mathutils import Vector

output = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
os.makedirs(output, exist_ok=True)
rig = bpy.data.objects['base.001']
rig.animation_data_create()
weight_repairs = {}
for collection in bpy.data.collections:
    collection.hide_viewport = False
    collection.hide_select = False

def reveal(layer):
    layer.exclude = False
    layer.hide_viewport = False
    for child in layer.children:
        reveal(child)

reveal(bpy.context.view_layer.layer_collection)
for obj in bpy.data.objects:
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.hide_render = False
    obj.hide_select = False
    obj.select_set(obj.type in {'ARMATURE', 'MESH'})
    # The source contains a stale, unassigned armature modifier on each cat.
    for modifier in list(obj.modifiers):
        if modifier.type == 'ARMATURE' and modifier.object is None:
            obj.modifiers.remove(modifier)
    if obj.type == 'MESH':
        bone_groups = {g.index for g in obj.vertex_groups if g.name in rig.data.bones}
        weighted = [v for v in obj.data.vertices if any(g.group in bone_groups and g.weight > 0 for g in v.groups)]
        missing = [v for v in obj.data.vertices if not any(g.group in bone_groups and g.weight > 0 for g in v.groups)]
        # Preserve neighbouring skin influence instead of Unity's arbitrary bone-0 fallback.
        for vertex in missing:
            nearest = min(weighted, key=lambda v: (v.co - vertex.co).length_squared)
            influences = [g for g in nearest.groups if g.group in bone_groups and g.weight > 0]
            total = sum(g.weight for g in influences)
            for influence in influences:
                obj.vertex_groups[influence.group].add([vertex.index], influence.weight / total, 'REPLACE')
        if missing:
            weight_repairs[obj.name] = [v.index for v in missing]

bpy.context.view_layer.objects.active = rig
tracks = rig.animation_data.nla_tracks
for track in list(tracks):
    tracks.remove(track)
rig.animation_data.action = None
for action in bpy.data.actions:
    track = tracks.new()
    track.name = action.name
    strip = track.strips.new(action.name, int(action.frame_range[0]), action)
    strip.action_slot = action.slots[0]
    track.mute = True

report = {'meshes': [], 'actions': [], 'fps': bpy.context.scene.render.fps, 'weight_repairs': weight_repairs}
print('SELECTED=' + json.dumps([o.name for o in bpy.context.selected_objects]))
rig.data.pose_position = 'REST'
bpy.context.view_layer.update()
for obj in bpy.data.objects:
    if obj.type != 'MESH':
        continue
    points = [obj.matrix_world @ Vector(p) for p in obj.bound_box]
    report['meshes'].append({
        'name': obj.name, 'min': [min(p[i] for p in points) for i in range(3)],
        'max': [max(p[i] for p in points) for i in range(3)],
        'vertices': len(obj.data.vertices),
        'groups': [g.name for g in obj.vertex_groups],
    })
rig.data.pose_position = 'POSE'
for track in tracks:
    track.mute = False

for action in bpy.data.actions:
    report['actions'].append({'name': action.name, 'range': list(action.frame_range)})

bpy.ops.export_scene.fbx(
    filepath=os.path.join(output, 'ProtaCat.fbx'),
    use_selection=True, object_types={'ARMATURE', 'MESH'},
    axis_forward='-Z', axis_up='Y', global_scale=1.0,
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
    add_leaf_bones=False, use_armature_deform_only=False,
    bake_anim=True, bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=True, bake_anim_use_all_actions=False,
    bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE',
)
with open(os.path.join(output, 'source-manifest.json'), 'w', encoding='utf-8') as f:
    json.dump(report, f, ensure_ascii=False, indent=2)
print('CAT_EXPORT=' + json.dumps(report))
