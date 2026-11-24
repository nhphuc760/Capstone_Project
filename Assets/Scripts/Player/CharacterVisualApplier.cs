using System.Collections.Generic;
using UnityEngine;

public class CharacterVisualApplier : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer targetRenderer;
    [SerializeField] private Transform rootBone;
    [SerializeField] private CharacterDatabase database;

    private Dictionary<string, Transform> _boneMap;

    private void Awake()
    {
        if (database == null)
            database = Resources.Load<CharacterDatabase>("CharacterDatabase");

        CacheBoneMap();
        ApplyCharacter(0);
    }

    private void CacheBoneMap()
    {
        if (_boneMap != null || rootBone == null) return;

        _boneMap = new Dictionary<string, Transform>();
        foreach (Transform bone in rootBone.GetComponentsInChildren<Transform>(true))
        {
            _boneMap[bone.name] = bone;
        }
    }

    public void ApplyCharacter(int index)
    {
        if (database == null)
            database = Resources.Load<CharacterDatabase>("CharacterDatabase");

        CharacterVisualData data = database != null ? database.GetCharacter(index) : null;
        if (data == null || targetRenderer == null) return;

        if (data.bodyMesh == null)
        {
            Debug.LogWarning($"[CharacterVisualApplier] Nhân vật ở index {index} ({data.characterName}) đang bị trống Body Mesh trong CharacterDatabase!");
            return;
        }

        CacheBoneMap();

        if (_boneMap != null && targetRenderer.bones != null && targetRenderer.bones.Length > 0)
        {
            Transform[] currentBones = targetRenderer.bones;
            Transform[] remappedBones = new Transform[currentBones.Length];
            for (int i = 0; i < currentBones.Length; i++)
            {
                if (currentBones[i] != null && _boneMap.TryGetValue(currentBones[i].name, out Transform mapped))
                    remappedBones[i] = mapped;
                else
                    remappedBones[i] = currentBones[i];
            }
            targetRenderer.bones = remappedBones;
        }

        if (rootBone != null)
            targetRenderer.rootBone = rootBone;

        targetRenderer.sharedMesh = data.bodyMesh;
        if (data.materials != null && data.materials.Length > 0)
        {
            targetRenderer.sharedMaterials = data.materials;
        }
    }
}