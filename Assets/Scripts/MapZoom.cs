using UnityEngine;

public class MapZoom : MonoBehaviour
{
    [Header("References")]
    public RectTransform content;
    public RectTransform viewport;
    public GameObject mapPanel;

    [Header("Zoom")]
    public float zoomSpeed = 0.2f;
    public float minZoom = 1f;
    public float maxZoom = 3f;

    [Header("Drag")]
    public float dragSpeed = 1f;
    // Giá trị zoom hiện tại
    private float currentZoom = 1f;

    private bool isDragging;
    // Lưu vị trí chuột ở frame trước
    private Vector2 lastMousePosition;

    void Update()
    {
        if (!mapPanel.activeSelf)
            return;

        HandleZoom();
        HandleDrag();
    }
    //Xử lí zoom khi cuộn chuột
    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        float oldZoom = currentZoom;

        currentZoom += scroll * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        if (Mathf.Approximately(oldZoom, currentZoom))
            return;

        // Lấy vị trí chuột trên Content trước khi zoom
        Vector2 beforeZoom;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content,
            Input.mousePosition,
            null,
            out beforeZoom);

        // Zoom
        content.localScale = Vector3.one * currentZoom;

        // Lấy lại vị trí sau khi zoom
        Vector2 afterZoom;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content,
            Input.mousePosition,
            null,
            out afterZoom);

        // Sai lệch
        Vector2 delta = afterZoom - beforeZoom;

        // Bù lại
        content.anchoredPosition += delta * currentZoom;

        ClampContent();
    }
    //Hàm xử lí kéo bán đồ bằng chuột
    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (!isDragging)
            return;

        //Khoảng cách chuột di chuyển
        Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;

        // Kéo thả để di chuyển bản đồ
        content.anchoredPosition += mouseDelta * dragSpeed;

        //Cập nhật vị trí con trỏ chuột 
        lastMousePosition = Input.mousePosition;

        ClampContent();
    }
    //Hàm này giới hạn vị trí của content để không vượt ra ngoài viewport
    void ClampContent()
    { 
        // Kích thước vùng hiển thị
        Vector2 viewSize = viewport.rect.size;
        // Kích thước Content sau khi zoom
        Vector2 contentSize = Vector2.Scale(content.rect.size, content.localScale);

        //Giới hạn kích thước content để không vượt quá viewport
        float limitX = Mathf.Max(0, (contentSize.x - viewSize.x) / 2f);
        float limitY = Mathf.Max(0, (contentSize.y - viewSize.y) / 2f);

        Vector2 pos = content.anchoredPosition;

        // Không cho kéo vượt biên
        pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
        pos.y = Mathf.Clamp(pos.y, -limitY, limitY);

        content.anchoredPosition = pos;
    }
}
