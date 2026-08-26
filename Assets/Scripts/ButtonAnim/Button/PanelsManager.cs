using UnityEngine;
using UnityEngine.UI;

public class PanelsManager : MonoBehaviour
{
    [SerializeField] private Button[] Buttons;
    [SerializeField] private GameObject[] Panels;

    private void Start()
    {
        // Tắt tất cả panel lúc bắt đầu
        CloseAllPanels();

        // Gán sự kiện cho từng button
        for (int i = 0; i < Buttons.Length; i++)
        {
            int index = i;
            Buttons[i].onClick.AddListener(() => OpenPanel(index));
        }
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPanels();
        }
    }

    private void OpenPanel(int index)
    {
        // Kiểm tra index hợp lệ
        if (index < 0 || index >= Panels.Length)
            return;

        // Đóng tất cả panel trước
        CloseAllPanels();

        // Mở panel tương ứng
        Panels[index].SetActive(true);
    }

    private void CloseAllPanels()
    {
        foreach (GameObject panel in Panels)
        {
            panel.SetActive(false);
        }
    }
}