using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class LobbyCharacterSelector : NetworkBehaviour
{
    public static readonly Dictionary<PlayerRef, int> SelectedByPlayer = new();
    public static int LocalSelectedIndex { get; private set; }

    [SerializeField] private CharacterVisualApplier visualApplier;

    [Networked, OnChangedRender(nameof(OnCharacterIndexChanged))]
    public int SelectedCharacterIndex { get; set; }

    public override void Spawned()
    {
        if (visualApplier == null)
            visualApplier = GetComponent<CharacterVisualApplier>();

        // Khôi phục lựa chọn gần nhất của người chơi local nếu có
        if (Object.HasInputAuthority)
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
            SelectCharacter(savedIndex);
        }

        ApplyAndCache(SelectedCharacterIndex);
    }

    /// <summary>
    /// Gắn hàm này vào nút bấm UI chọn nhân vật (truyền index 0, 1, 2...)
    /// </summary>
    public void SelectCharacter(int index)
    {
        if (!Object.HasInputAuthority) return;

        LocalSelectedIndex = index;
        PlayerPrefs.SetInt("SelectedCharacterIndex", index);

        if (Object.HasStateAuthority)
            SelectedCharacterIndex = index;
        else
            RPC_RequestSelectCharacter(index);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSelectCharacter(int index)
    {
        SelectedCharacterIndex = index;
    }

    private void OnCharacterIndexChanged()
    {
        ApplyAndCache(SelectedCharacterIndex);
    }

    private void ApplyAndCache(int index)
    {
        SelectedByPlayer[Object.InputAuthority] = index;
        if (visualApplier != null)
            visualApplier.ApplyCharacter(index);
    }
}