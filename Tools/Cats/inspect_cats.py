import bpy
import json

report = {
    "version": bpy.app.version_string,
    "objects": [{
        "name": o.name, "type": o.type,
        "parent": o.parent.name if o.parent else None,
        "location": list(o.location), "scale": list(o.scale),
        "dimensions": list(o.dimensions),
        "materials": [s.material.name if s.material else None for s in o.material_slots],
        "modifiers": [{"type": m.type, "object": m.object.name if getattr(m, "object", None) else None} for m in o.modifiers],
        "bones": [b.name for b in o.data.bones] if o.type == 'ARMATURE' else [],
        "action": o.animation_data.action.name if o.animation_data and o.animation_data.action else None,
    } for o in bpy.data.objects],
    "actions": [{"name": a.name, "range": list(a.frame_range), "slots": [s.identifier for s in a.slots]} for a in bpy.data.actions],
    "images": [{"name": i.name, "path": i.filepath, "packed": bool(i.packed_file)} for i in bpy.data.images],
    "materials": [{"name": m.name, "color": list(m.diffuse_color), "nodes": [{"name": n.name, "type": n.type} for n in m.node_tree.nodes] if m.node_tree else []} for m in bpy.data.materials],
    "fps": bpy.context.scene.render.fps,
}
print('CAT_INSPECTION=' + json.dumps(report))
